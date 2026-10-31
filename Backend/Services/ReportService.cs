using System.Text.Json;
using Backend.Handler;
using Data;
using DTOs.Report;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Report;
using Npgsql;

namespace Backend.Services;

public interface IReportService
{
    /// <summary>
    /// Relatório de ocupação e ociosidade de um mês ou de uma sequência de meses já encerrados.
    /// Usa o relatório gravado quando existir; caso contrário calcula, grava e devolve.
    /// </summary>
    public Task<ReportResponseDTO> GetReport(ReportRequestDTO query);
}

public class ReportService(AppDbContext dbCtx) : IReportService
{
    /// <summary>Suba este número quando mudar o cálculo ou o formato do DTO: os relatórios antigos são refeitos.</summary>
    private const int CacheVersion = 2;

    /// <summary>Limite de meses por consulta, para uma única requisição não varrer anos de reservas.</summary>
    private const int MaxRangeMonths = 60;

    /// <summary>O Brasil não tem mais horário de verão, então um deslocamento fixo evita depender do tzdata do servidor.</summary>
    private static readonly TimeSpan BrazilOffset = TimeSpan.FromHours(-3);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ReportResponseDTO> GetReport(ReportRequestDTO query)
    {
        var (rangeStart, lastMonth) = ResolveRange(query);

        var today = DateOnly.FromDateTime(DateTime.UtcNow + BrazilOffset);

        // Sem o mês fechado os dados ainda mudam (confirmações, cancelamentos, novas reservas).
        if (lastMonth >= new DateOnly(today.Year, today.Month, 1))
        {
            throw new BadRequestException("O relatório só está disponível para meses já encerrados.");
        }

        var stored = await dbCtx.Reports.FirstOrDefaultAsync(r =>
            r.FromYear == rangeStart.Year && r.FromMonth == rangeStart.Month
            && r.ToYear == lastMonth.Year && r.ToMonth == lastMonth.Month);

        if (stored != null && !query.Refresh && stored.Version == CacheVersion)
        {
            var cached = TryDeserialize(stored.Data);
            if (cached != null)
            {
                return cached;
            }
        }

        var report = await BuildReport(rangeStart, lastMonth, today);
        var json = JsonSerializer.Serialize(report, JsonOptions);

        if (stored == null)
        {
            dbCtx.Reports.Add(new ReportModel
            {
                FromYear = rangeStart.Year,
                FromMonth = rangeStart.Month,
                ToYear = lastMonth.Year,
                ToMonth = lastMonth.Month,
                Version = CacheVersion,
                GeneratedAt = report.GeneratedAt,
                Data = json
            });
        }
        else
        {
            stored.Version = CacheVersion;
            stored.GeneratedAt = report.GeneratedAt;
            stored.Data = json;
        }

        try
        {
            await dbCtx.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (stored == null
            && ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Duas requisições calcularam o mesmo período ao mesmo tempo e a outra gravou primeiro.
            // O relatório calculado aqui continua válido; só não precisa ser gravado de novo.
        }

        return report;
    }

    /// <summary>
    /// Valida o período pedido e devolve o primeiro dia do mês inicial e o primeiro dia do mês final.
    /// Sem fim informado, o período é só o mês inicial.
    /// </summary>
    private static (DateOnly FirstMonth, DateOnly LastMonth) ResolveRange(ReportRequestDTO query)
    {
        if (query.FromYear < 2000 || query.FromYear > 9999 || query.FromMonth < 1 || query.FromMonth > 12)
        {
            throw new BadRequestException("Mês ou ano inicial inválido.");
        }

        if (query.ToYear.HasValue != query.ToMonth.HasValue)
        {
            throw new BadRequestException("Informe o ano e o mês finais juntos, ou nenhum dos dois.");
        }

        var toYear = query.ToYear ?? query.FromYear;
        var toMonth = query.ToMonth ?? query.FromMonth;

        if (toYear < 2000 || toYear > 9999 || toMonth < 1 || toMonth > 12)
        {
            throw new BadRequestException("Mês ou ano final inválido.");
        }

        var first = new DateOnly(query.FromYear, query.FromMonth, 1);
        var last = new DateOnly(toYear, toMonth, 1);

        if (last < first)
        {
            throw new BadRequestException("O mês final não pode ser anterior ao mês inicial.");
        }

        var months = (last.Year - first.Year) * 12 + last.Month - first.Month + 1;
        if (months > MaxRangeMonths)
        {
            throw new BadRequestException($"O período do relatório pode ter no máximo {MaxRangeMonths} meses.");
        }

        return (first, last);
    }

    private static ReportResponseDTO? TryDeserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ReportResponseDTO>(json, JsonOptions);
        }
        catch (JsonException)
        {
            // Dado gravado ilegível: trata como se não existisse e recalcula.
            return null;
        }
    }

    private async Task<ReportResponseDTO> BuildReport(DateOnly rangeStart, DateOnly lastMonth, DateOnly today)
    {
        // Do primeiro dia do mês inicial ao último dia do mês final.
        var rangeEnd = lastMonth.AddMonths(1).AddDays(-1);
        var daysInRange = rangeEnd.DayNumber - rangeStart.DayNumber + 1;

        // Reservas que tocam o período. Canceladas, recusadas e bloqueadas nunca entram.
        var rows = await dbCtx.SpaceReserves.AsNoTracking()
            .Where(sr => sr.DateFrom <= rangeEnd && sr.DateTo >= rangeStart)
            .Where(sr => sr.Status == EReserveStatus.Booking || sr.Status == EReserveStatus.Confirmed)
            .Select(sr => new ReportReserveRow(
                sr.Id,
                sr.FkSpaceId,
                sr.Space.Name,
                sr.FkAccountId,
                sr.Account.UserName,
                sr.Account.Registration,
                sr.DateFrom,
                sr.DateTo,
                sr.Status,
                sr.SpaceReserveSchedules.Select(s => s.FkScheduleId).ToList()))
            .ToListAsync();

        var spaces = await dbCtx.Spaces.AsNoTracking()
            .Select(s => new ReportSpaceRow(s.Id, s.Name, s.Locked))
            .ToListAsync();

        var enabledScheduleIds = await dbCtx.Schedules.AsNoTracking()
            .Where(s => s.Enabled)
            .Select(s => s.Id)
            .ToListAsync();

        var counted = new List<ReportCountedReserve>();
        foreach (var row in rows)
        {
            var status = Classify(row, today);
            if (status == null)
            {
                continue;
            }

            // Só os dias que caem dentro do período contam para a ocupação dele.
            var from = row.DateFrom > rangeStart ? row.DateFrom : rangeStart;
            var to = row.DateTo < rangeEnd ? row.DateTo : rangeEnd;
            var days = Math.Max(0, to.DayNumber - from.DayNumber + 1);

            counted.Add(new ReportCountedReserve(row, status.Value, days * row.ScheduleIds.Count));
        }

        // Capacidade de cada espaço no período = horários por dia x dias do período. Horários desativados depois
        // que a reserva foi feita continuam valendo para quem os usou, então entram na conta.
        var slotsPerDay = enabledScheduleIds
            .Concat(counted.SelectMany(c => c.Row.ScheduleIds))
            .Distinct()
            .Count();
        double capacity = (double)slotsPerDay * daysInRange;

        // Espaço bloqueado só aparece se teve reserva contabilizada; os demais entram mesmo sem reservas
        // (0% de ocupação), senão o "menos disputado" nunca seria um espaço parado.
        var countedSpaceIds = counted.Select(c => c.Row.SpaceId).ToHashSet();
        var bySpace = counted.ToLookup(c => c.Row.SpaceId);

        var spaceStats = spaces
            .Where(s => !s.Locked || countedSpaceIds.Contains(s.Id))
            .Select(space =>
            {
                var items = bySpace[space.Id].ToList();
                var absent = items.Count(i => i.Status == EReportReserveStatus.Absent);
                var slotDays = items.Sum(i => i.SlotDays);

                return new ReportSpaceStatDTO(
                    space.Id,
                    space.Name,
                    Percent(slotDays, capacity),
                    items.Count,
                    items.Count - absent,
                    absent,
                    Percent(absent, items.Count));
            })
            .ToList();

        // Empates: cada ranking devolve todos que empatam no critério (comparado já arredondado, como aparece na tela),
        // sempre ordenados pelo nome para o mesmo período dar sempre o mesmo resultado.
        var highestDemand = TopTied(
            spaceStats.Where(s => s.OccupancyRate > 0),
            s => s.OccupancyRate,
            s => s.Name);

        // Quando todos os espaços empatam na mesma taxa não existe um "menos disputado" diferente do "mais disputado".
        var lowestDemand = spaceStats.Count < 2
            ? new List<ReportSpaceStatDTO>()
            : TopTied(spaceStats, s => s.OccupancyRate, s => s.Name, descending: false);
        if (highestDemand.Count > 0 && lowestDemand.Count > 0
            && lowestDemand[0].OccupancyRate >= highestDemand[0].OccupancyRate)
        {
            lowestDemand = [];
        }

        var mostIdleSpaces = TopTied(
            spaceStats.Where(s => s.AbsentReserves > 0),
            s => (s.AbsentReserves, s.IdleRate),
            s => s.Name);

        var userStats = counted
            .GroupBy(c => c.Row.AccountId)
            .Select(group =>
            {
                var first = group.First().Row;
                var total = group.Count();
                var absent = group.Count(i => i.Status == EReportReserveStatus.Absent);

                return new ReportUserStatDTO(
                    group.Key,
                    first.UserName ?? "Sem nome",
                    first.Registration,
                    total,
                    total - absent,
                    absent,
                    Percent(absent, total));
            })
            .ToList();

        var mostReservesUsers = TopTied(userStats, u => u.TotalReserves, u => u.Name);

        var mostIdleUsers = TopTied(
            userStats.Where(u => u.AbsentReserves > 0),
            u => (u.AbsentReserves, u.IdleRate),
            u => u.Name);

        var totalAbsent = counted.Count(c => c.Status == EReportReserveStatus.Absent);
        var averageOccupancy = spaceStats.Count == 0
            ? 0
            : Math.Round(spaceStats.Average(s => s.OccupancyRate), 1, MidpointRounding.AwayFromZero);

        var reserves = counted
            .OrderBy(c => c.Row.DateFrom)
            .ThenBy(c => c.Row.SpaceName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Row.Id)
            .Select(c => new ReportReserveDTO(
                c.Row.Id,
                c.Row.SpaceName,
                c.Row.UserName ?? "Sem nome",
                c.Row.DateFrom,
                c.Row.DateTo,
                c.Status))
            .ToList();

        return new ReportResponseDTO(
            rangeStart.Year,
            rangeStart.Month,
            lastMonth.Year,
            lastMonth.Month,
            DateTimeOffset.UtcNow,
            new ReportSummaryDTO(
                averageOccupancy,
                Percent(totalAbsent, counted.Count),
                counted.Count,
                counted.Count - totalAbsent,
                totalAbsent,
                highestDemand),
            new ReportRankingDTO(highestDemand, lowestDemand, mostIdleSpaces),
            new ReportUsersDTO(mostReservesUsers, mostIdleUsers),
            reserves);
    }

    /// <summary>
    /// Regras do relatório:
    /// confirmada = Confirmada;
    /// em "Booking" cuja data de início já passou = Ausente (a confirmação deveria ter chegado antes da reserva);
    /// em "Booking" que ainda não chegou ao dia = fora do relatório (retorna null).
    /// </summary>
    private static EReportReserveStatus? Classify(ReportReserveRow row, DateOnly today)
    {
        if (row.Status == EReserveStatus.Confirmed)
        {
            return EReportReserveStatus.Confirmed;
        }

        return row.DateFrom < today ? EReportReserveStatus.Absent : null;
    }

    /// <summary>
    /// Todos os itens que empatam no melhor valor de <paramref name="key"/> (o maior, ou o menor com
    /// <paramref name="descending"/> falso), em ordem alfabética. Lista vazia quando não há itens.
    /// </summary>
    private static List<T> TopTied<T, TKey>(
        IEnumerable<T> items,
        Func<T, TKey> key,
        Func<T, string> name,
        bool descending = true) where TKey : IComparable<TKey>
    {
        var list = items.ToList();
        if (list.Count == 0)
        {
            return [];
        }

        var best = descending ? list.Max(key)! : list.Min(key)!;

        return list
            .Where(item => key(item).CompareTo(best) == 0)
            .OrderBy(name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Porcentagem de 0 a 100 com uma casa decimal (0 quando o total é zero).</summary>
    private static double Percent(double part, double total)
    {
        if (total <= 0)
        {
            return 0;
        }

        return Math.Round(Math.Min(part / total * 100, 100), 1, MidpointRounding.AwayFromZero);
    }
}

internal sealed record ReportReserveRow(
    long Id,
    long SpaceId,
    string SpaceName,
    string AccountId,
    string? UserName,
    string Registration,
    DateOnly DateFrom,
    DateOnly DateTo,
    EReserveStatus Status,
    List<long> ScheduleIds);

internal sealed record ReportSpaceRow(long Id, string Name, bool Locked);

/// <param name="SlotDays">Horários ocupados no mês: dias da reserva dentro do mês x horários da reserva.</param>
internal sealed record ReportCountedReserve(ReportReserveRow Row, EReportReserveStatus Status, int SlotDays);
