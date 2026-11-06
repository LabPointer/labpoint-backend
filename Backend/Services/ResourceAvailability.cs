using System.Linq.Expressions;
using Backend.Handler;
using Data;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Reserve;
using Models.Resource;

namespace Backend.Services;

/// <summary>Unidades de um recurso pedidas numa reserva.</summary>
public sealed record ResourceDemand(long ResourceId, int Quantity);

/// <summary>Reservas que não contam como ocupação (a própria reserva, durante uma edição).</summary>
public sealed record ReserveExclusion(long? SpaceReserveId = null, long? ResourceReserveId = null)
{
    public static readonly ReserveExclusion None = new();
}

/// <summary>
/// Disponibilidade de recursos reserváveis. Um recurso tem <c>Quantity</c> unidades; cada reserva ativa ocupa
/// <c>Quantity</c> delas em cada (dia, horário) do seu período. Há conflito quando, em algum (dia, horário),
/// a soma das unidades ocupadas mais as pedidas passa do total.
/// A soma é feita em memória: os períodos são curtos (a UI limita a 30 dias) e só entram reservas do mesmo recurso
/// que se sobrepõem em data.
/// </summary>
public static class ResourceAvailability
{
    /// <summary>Só reservas pendentes ou confirmadas ocupam unidades (mesma regra das salas).</summary>
    public static readonly Expression<Func<ResourceReserveModel, bool>> IsActive =
        rr => rr.Status == EReserveStatus.Booking || rr.Status == EReserveStatus.Confirmed;

    public static bool IsActiveStatus(EReserveStatus status) =>
        status is EReserveStatus.Booking or EReserveStatus.Confirmed;

    /// <summary>
    /// Busca em nome e descrição: full-text em português (palavras inteiras, com radical: "notebooks" acha "notebook")
    /// OU trecho parcial sem diferenciar maiúsculas ("note" acha "Notebooks").
    /// </summary>
    public static Expression<Func<ResourceModel, bool>> MatchesText(string term)
    {
        var text = term.Trim();
        var pattern = QueryHelpers.ContainsPattern(text);
        return r =>
            EF.Functions.ToTsVector("portuguese", r.Name + " " + (r.Description ?? ""))
                .Matches(EF.Functions.WebSearchToTsQuery("portuguese", text))
            || EF.Functions.ILike(r.Name, pattern)
            || (r.Description != null && EF.Functions.ILike(r.Description, pattern));
    }

    /// <summary>Mesma busca de <see cref="MatchesText"/>, aplicada ao recurso de uma reserva.</summary>
    public static Expression<Func<ResourceReserveModel, bool>> ReserveMatchesText(string term)
    {
        var text = term.Trim();
        var pattern = QueryHelpers.ContainsPattern(text);
        return rr =>
            EF.Functions.ToTsVector("portuguese", rr.Resource.Name + " " + (rr.Resource.Description ?? ""))
                .Matches(EF.Functions.WebSearchToTsQuery("portuguese", text))
            || EF.Functions.ILike(rr.Resource.Name, pattern)
            || (rr.Resource.Description != null && EF.Functions.ILike(rr.Resource.Description, pattern));
    }

    /// <summary>
    /// Unidades ocupadas por (recurso, dia, horário) dentro de [<paramref name="start"/>, <paramref name="end"/>].
    /// <paramref name="scheduleIds"/> nulo = todos os horários.
    /// </summary>
    public static async Task<Dictionary<(long ResourceId, DateOnly Day, long ScheduleId), int>> LoadUsage(
        AppDbContext dbCtx,
        IReadOnlyCollection<long> resourceIds,
        DateOnly start,
        DateOnly end,
        IReadOnlySet<long>? scheduleIds,
        ReserveExclusion exclusion)
    {
        var usage = new Dictionary<(long, DateOnly, long), int>();
        if (resourceIds.Count == 0)
        {
            return usage;
        }

        var query = dbCtx.ResourceReserves.AsNoTracking()
            .Where(rr => resourceIds.Contains(rr.FkResourceId) && rr.DateFrom <= end && rr.DateTo >= start)
            .Where(IsActive);

        if (exclusion.ResourceReserveId is { } excludedReserveId)
        {
            query = query.Where(rr => rr.Id != excludedReserveId);
        }

        if (exclusion.SpaceReserveId is { } excludedSpaceReserveId)
        {
            query = query.Where(rr => rr.FkSpaceReserveId == null || rr.FkSpaceReserveId != excludedSpaceReserveId);
        }

        if (scheduleIds is { Count: > 0 })
        {
            // lista local: o EF traduz List.Contains para "= ANY(...)" (IReadOnlySet.Contains não é garantido)
            var scheduleList = scheduleIds.ToList();
            query = query.Where(rr => rr.ResourceReserveSchedules.Any(s => scheduleList.Contains(s.FkScheduleId)));
        }

        var rows = await query
            .Select(rr => new
            {
                rr.FkResourceId,
                rr.DateFrom,
                rr.DateTo,
                rr.Quantity,
                Schedules = rr.ResourceReserveSchedules.Select(s => s.FkScheduleId).ToList()
            })
            .AsSplitQuery()
            .ToListAsync();

        foreach (var row in rows)
        {
            var from = row.DateFrom > start ? row.DateFrom : start;
            var to = row.DateTo < end ? row.DateTo : end;

            for (var day = from; day <= to; day = day.AddDays(1))
            {
                foreach (var scheduleId in row.Schedules)
                {
                    if (scheduleIds is { Count: > 0 } && !scheduleIds.Contains(scheduleId))
                    {
                        continue;
                    }

                    var key = (row.FkResourceId, day, scheduleId);
                    usage[key] = usage.GetValueOrDefault(key) + row.Quantity;
                }
            }
        }

        return usage;
    }

    /// <summary>Maior ocupação de um recurso em um (dia, horário) qualquer do mapa.</summary>
    public static int PeakUsage(IReadOnlyDictionary<(long ResourceId, DateOnly Day, long ScheduleId), int> usage, long resourceId) =>
        usage.Where(entry => entry.Key.ResourceId == resourceId).Select(entry => entry.Value).DefaultIfEmpty(0).Max();

    /// <summary>
    /// Garante que os recursos pedidos existem, estão ativos, são reserváveis e têm unidades livres em todos os
    /// (dia, horário) do período. Pedidos repetidos do mesmo recurso são somados.
    /// </summary>
    public static async Task EnsureAvailable(
        AppDbContext dbCtx,
        IEnumerable<ResourceDemand> demands,
        DateOnly start,
        DateOnly end,
        IReadOnlySet<long> scheduleIds,
        ReserveExclusion exclusion)
    {
        var merged = MergeDemands(demands);
        if (merged.Count == 0)
        {
            return;
        }

        var ids = merged.Keys.ToList();
        var resources = await dbCtx.Resources.AsNoTracking()
            .Where(r => ids.Contains(r.Id))
            .Select(r => new { r.Id, r.Name, r.Enabled, r.Reservable, r.Quantity })
            .ToListAsync();

        foreach (var id in ids)
        {
            var resource = resources.FirstOrDefault(r => r.Id == id)
                ?? throw new ResourceNotFoundException("Recurso não encontrado.");

            if (!resource.Reservable || resource.Quantity is not > 0)
            {
                throw new BadRequestException($"O recurso '{resource.Name}' não é reservável.");
            }

            if (!resource.Enabled)
            {
                throw new BadRequestException($"O recurso '{resource.Name}' está desativado.");
            }

            if (merged[id] > resource.Quantity)
            {
                throw new BadRequestException(
                    $"O recurso '{resource.Name}' tem apenas {resource.Quantity} unidade(s) no total.");
            }
        }

        var usage = await LoadUsage(dbCtx, ids, start, end, scheduleIds, exclusion);

        // Primeiro conflito encontrado (o mais cedo), para a mensagem apontar dia e horário exatos.
        var conflict = usage
            .Select(entry => new
            {
                entry.Key.ResourceId,
                entry.Key.Day,
                entry.Key.ScheduleId,
                Free = resources.First(r => r.Id == entry.Key.ResourceId).Quantity!.Value - entry.Value
            })
            .Where(item => item.Free < merged[item.ResourceId])
            .OrderBy(item => item.Day)
            .FirstOrDefault();

        if (conflict == null)
        {
            return;
        }

        var name = resources.First(r => r.Id == conflict.ResourceId).Name;
        var schedule = await dbCtx.Schedules.AsNoTracking()
            .Where(s => s.Id == conflict.ScheduleId)
            .Select(s => new { s.StartAt, s.EndAt })
            .FirstOrDefaultAsync();
        var when = schedule == null
            ? $"{conflict.Day:dd/MM/yyyy}"
            : $"{conflict.Day:dd/MM/yyyy} ({schedule.StartAt:HH\\:mm} às {schedule.EndAt:HH\\:mm})";
        var free = Math.Max(conflict.Free, 0);

        throw new BadRequestException(
            $"O recurso '{name}' não tem unidades suficientes em {when}: {free} livre(s), {merged[conflict.ResourceId]} pedida(s).");
    }

    /// <summary>
    /// Menor quantidade livre de cada horário ativo no período (para o front esconder horários sem unidades).
    /// </summary>
    public static async Task<List<(long ScheduleId, int Available)>> FreeBySchedule(
        AppDbContext dbCtx,
        long resourceId,
        DateOnly start,
        DateOnly end,
        ReserveExclusion exclusion)
    {
        var resource = await dbCtx.Resources.AsNoTracking()
            .Where(r => r.Id == resourceId)
            .Select(r => new { r.Reservable, r.Quantity })
            .FirstOrDefaultAsync()
            ?? throw new ResourceNotFoundException("Recurso não encontrado.");

        if (!resource.Reservable || resource.Quantity is not > 0)
        {
            throw new BadRequestException("O recurso informado não é reservável.");
        }

        var scheduleIds = await dbCtx.Schedules.AsNoTracking()
            .Where(s => s.Enabled)
            .Select(s => s.Id)
            .ToListAsync();

        var usage = await LoadUsage(dbCtx, [resourceId], start, end, null, exclusion);

        return scheduleIds
            .Select(scheduleId =>
            {
                var peak = usage
                    .Where(entry => entry.Key.ScheduleId == scheduleId)
                    .Select(entry => entry.Value)
                    .DefaultIfEmpty(0)
                    .Max();
                return (scheduleId, Math.Max(resource.Quantity!.Value - peak, 0));
            })
            .ToList();
    }

    public static Dictionary<long, int> MergeDemands(IEnumerable<ResourceDemand> demands)
    {
        var merged = new Dictionary<long, int>();
        foreach (var demand in demands)
        {
            if (demand.Quantity < 1)
            {
                throw new BadRequestException("A quantidade de cada recurso deve ser maior que zero.");
            }

            merged[demand.ResourceId] = merged.GetValueOrDefault(demand.ResourceId) + demand.Quantity;
        }

        return merged;
    }
}
