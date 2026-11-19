using System.Security.Cryptography;
using System.Text;
using Data;
using DTOs.Schedule;
using DTOs.Usage;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Usage;

namespace Backend.Services;

/// <summary>Horário de uma reserva com o turno a que pertence.</summary>
public sealed record ShiftSlot(long ScheduleId, TimeOnly StartAt, TimeOnly EndAt, EShift Shift);

/// <summary>Janelas de check-in e check-out de uma sessão.</summary>
public sealed record UsageWindows(
    DateTimeOffset CheckInOpensAt,
    DateTimeOffset CheckInClosesAt,
    DateTimeOffset CheckOutOpensAt,
    DateTimeOffset CheckOutClosesAt);

/// <summary>
/// Regras do check-in/check-out por turno:
/// check-in abre 15 min antes do 1º horário do turno e fecha 30 min depois do início dele (e-mail 30 min antes);
/// check-out abre no início do último horário e fecha 60 min depois do fim dele (e-mail na abertura).
/// Sem check-in no prazo a sessão fica ausente e os horários daquele turno são liberados.
/// </summary>
public static class UsageRules
{
    public static readonly TimeSpan CheckInOpensBefore = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan CheckInClosesAfter = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan CheckInEmailBefore = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan CheckOutClosesAfter = TimeSpan.FromMinutes(60);

    /// <summary>Folga de check-in para sessões criadas depois do prazo (reserva feita em cima da hora).</summary>
    public static readonly TimeSpan LateCheckInGrace = TimeSpan.FromMinutes(30);

    public static UsageWindows ComputeWindows(DateOnly date, IReadOnlyCollection<ShiftSlot> slots)
    {
        var first = slots.MinBy(s => s.StartAt)!;
        var last = slots.MaxBy(s => s.EndAt)!;
        var firstStart = BrazilClock.At(date, first.StartAt);

        return new UsageWindows(
            firstStart - CheckInOpensBefore,
            firstStart + CheckInClosesAfter,
            BrazilClock.At(date, last.StartAt),
            BrazilClock.At(date, last.EndAt) + CheckOutClosesAfter);
    }

    /// <summary>Check-in feito (em uso, encerrada ou aguardando o admin): conta como presença.</summary>
    public static bool IsPresent(EUsageStatus status) =>
        status is EUsageStatus.CheckedIn or EUsageStatus.Completed or EUsageStatus.CheckoutPending;

    /// <summary>Token aleatório (base64url, vai no link) e o hash dele (vai para o banco).</summary>
    public static (string Token, string Hash) NewToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return (token, HashToken(token));
    }

    public static string HashToken(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    /// <summary>(reserva de sala, dia, turno) ausentes no período: esses horários não ocupam mais a sala.</summary>
    public static async Task<HashSet<(long ReserveId, DateOnly Day, EShift Shift)>> LoadAbsentSpace(
        AppDbContext dbCtx, DateOnly start, DateOnly end)
    {
        var rows = await dbCtx.ReserveUsages.AsNoTracking()
            .Where(u => u.Status == EUsageStatus.Absent && u.FkSpaceReserveId != null && u.Date >= start && u.Date <= end)
            .Select(u => new { ReserveId = u.FkSpaceReserveId!.Value, u.Date, u.Shift })
            .ToListAsync();

        return rows.Select(r => (r.ReserveId, r.Date, r.Shift)).ToHashSet();
    }

    /// <summary>(reserva de recurso avulsa, dia, turno) ausentes no período.</summary>
    public static async Task<HashSet<(long ReserveId, DateOnly Day, EShift Shift)>> LoadAbsentResource(
        AppDbContext dbCtx, DateOnly start, DateOnly end)
    {
        var rows = await dbCtx.ReserveUsages.AsNoTracking()
            .Where(u => u.Status == EUsageStatus.Absent && u.FkResourceReserveId != null && u.Date >= start && u.Date <= end)
            .Select(u => new { ReserveId = u.FkResourceReserveId!.Value, u.Date, u.Shift })
            .ToListAsync();

        return rows.Select(r => (r.ReserveId, r.Date, r.Shift)).ToHashSet();
    }

    /// <summary>Contagem das sessões por reserva de sala.</summary>
    public static async Task<Dictionary<long, UsageSummaryDTO>> LoadSpaceSummaries(
        AppDbContext dbCtx, IReadOnlyCollection<long> spaceReserveIds)
    {
        if (spaceReserveIds.Count == 0)
        {
            return [];
        }

        var ids = spaceReserveIds.ToList();
        var rows = await dbCtx.ReserveUsages.AsNoTracking()
            .Where(u => u.FkSpaceReserveId != null && ids.Contains(u.FkSpaceReserveId.Value))
            .GroupBy(u => new { ReserveId = u.FkSpaceReserveId!.Value, u.Status })
            .Select(g => new { g.Key.ReserveId, g.Key.Status, Count = g.Count() })
            .ToListAsync();

        return rows
            .GroupBy(r => r.ReserveId)
            .ToDictionary(g => g.Key, g => Summarize(g.Select(r => (r.Status, r.Count))));
    }

    /// <summary>Contagem das sessões por reserva de recurso (a vinculada usa as sessões da sala).</summary>
    public static async Task<Dictionary<long, UsageSummaryDTO>> LoadResourceSummaries(
        AppDbContext dbCtx, IReadOnlyCollection<(long Id, long? SpaceReserveId)> reserves)
    {
        if (reserves.Count == 0)
        {
            return [];
        }

        var standaloneIds = reserves.Where(r => r.SpaceReserveId == null).Select(r => r.Id).ToList();
        var standaloneRows = await dbCtx.ReserveUsages.AsNoTracking()
            .Where(u => u.FkResourceReserveId != null && standaloneIds.Contains(u.FkResourceReserveId.Value))
            .GroupBy(u => new { ReserveId = u.FkResourceReserveId!.Value, u.Status })
            .Select(g => new { g.Key.ReserveId, g.Key.Status, Count = g.Count() })
            .ToListAsync();

        var standalone = standaloneRows
            .GroupBy(r => r.ReserveId)
            .ToDictionary(g => g.Key, g => Summarize(g.Select(r => (r.Status, r.Count))));

        var spaces = await LoadSpaceSummaries(
            dbCtx,
            reserves.Where(r => r.SpaceReserveId != null).Select(r => r.SpaceReserveId!.Value).Distinct().ToList());

        return reserves.ToDictionary(
            r => r.Id,
            r => r.SpaceReserveId is { } spaceReserveId
                ? spaces.GetValueOrDefault(spaceReserveId, UsageSummaryDTO.Empty)
                : standalone.GetValueOrDefault(r.Id, UsageSummaryDTO.Empty));
    }

    private static UsageSummaryDTO Summarize(IEnumerable<(EUsageStatus Status, int Count)> counts)
    {
        var map = counts.ToDictionary(c => c.Status, c => c.Count);
        return new UsageSummaryDTO(
            map.Values.Sum(),
            map.GetValueOrDefault(EUsageStatus.Scheduled),
            map.GetValueOrDefault(EUsageStatus.CheckedIn),
            map.GetValueOrDefault(EUsageStatus.CheckoutPending),
            map.GetValueOrDefault(EUsageStatus.Completed),
            map.GetValueOrDefault(EUsageStatus.Absent));
    }
}

/// <summary>Linha projetada de uma sessão, com o e-mail do dono (usado nos avisos).</summary>
internal sealed record UsageRow(
    long Id,
    long? SpaceReserveId,
    long? ResourceReserveId,
    string? SpaceName,
    List<UsageResourceDTO> LinkedResources,
    UsageResourceDTO? StandaloneResource,
    UsageAccountDTO Account,
    string? Email,
    DateOnly Date,
    EShift Shift,
    EUsageStatus Status,
    List<ScheduleResponseDTO> SpaceSchedules,
    List<ScheduleResponseDTO> ResourceSchedules,
    DateTimeOffset CheckInOpensAt,
    DateTimeOffset CheckInClosesAt,
    DateTimeOffset CheckOutOpensAt,
    DateTimeOffset CheckOutClosesAt,
    DateTimeOffset? CheckedInAt,
    DateTimeOffset? CheckedOutAt,
    UsageAccountDTO? CheckedOutBy,
    List<UsageEntryResponseDTO> Entries)
{
    public List<ScheduleResponseDTO> Schedules =>
        SpaceSchedules.Concat(ResourceSchedules).OrderBy(s => s.StartAt).ToList();

    public string Place => SpaceName ?? StandaloneResource?.Name ?? "Reserva";

    public UsageSessionResponseDTO ToDto() => new(
        Id,
        SpaceReserveId,
        ResourceReserveId,
        SpaceName,
        StandaloneResource != null ? [StandaloneResource] : LinkedResources,
        Account,
        Date,
        Shift,
        Status,
        Schedules,
        CheckInOpensAt,
        CheckInClosesAt,
        CheckOutOpensAt,
        CheckOutClosesAt,
        CheckedInAt,
        CheckedOutAt,
        CheckedOutBy,
        Entries.OrderBy(e => Schedules.FindIndex(s => s.Id == e.ScheduleId)).ToList());
}

internal static class UsageQueries
{
    /// <summary>
    /// Projeta sessões com os horários da reserva naquele turno, recursos, dono e relatório. Os horários vêm da
    /// reserva de sala ou da de recurso (a outra lista vem vazia).
    /// </summary>
    public static IQueryable<UsageRow> Project(IQueryable<ReserveUsageModel> query) =>
        query
            .Select(u => new UsageRow(
                u.Id,
                u.FkSpaceReserveId,
                u.FkResourceReserveId,
                u.SpaceReserve == null ? null : u.SpaceReserve.Space.Name,
                u.SpaceReserve == null
                    ? new List<UsageResourceDTO>()
                    : u.SpaceReserve.ResourceReserves
                        .Where(rr => rr.Status == EReserveStatus.Booking || rr.Status == EReserveStatus.Confirmed)
                        .OrderBy(rr => rr.Resource.Name)
                        .Select(rr => new UsageResourceDTO(rr.FkResourceId, rr.Resource.Name, rr.Quantity))
                        .ToList(),
                u.ResourceReserve == null
                    ? null
                    : new UsageResourceDTO(
                        u.ResourceReserve.FkResourceId, u.ResourceReserve.Resource.Name, u.ResourceReserve.Quantity),
                new UsageAccountDTO(u.Account.Id, u.Account.UserName, u.Account.Registration),
                u.Account.Email,
                u.Date,
                u.Shift,
                u.Status,
                u.SpaceReserve == null
                    ? new List<ScheduleResponseDTO>()
                    : u.SpaceReserve.SpaceReserveSchedules
                        .Where(s => s.Schedule.Shift == u.Shift)
                        .Select(s => new ScheduleResponseDTO(
                            s.Schedule.Id, s.Schedule.StartAt, s.Schedule.EndAt, s.Schedule.Shift, s.Schedule.Enabled))
                        .ToList(),
                u.ResourceReserve == null
                    ? new List<ScheduleResponseDTO>()
                    : u.ResourceReserve.ResourceReserveSchedules
                        .Where(s => s.Schedule.Shift == u.Shift)
                        .Select(s => new ScheduleResponseDTO(
                            s.Schedule.Id, s.Schedule.StartAt, s.Schedule.EndAt, s.Schedule.Shift, s.Schedule.Enabled))
                        .ToList(),
                u.CheckInOpensAt,
                u.CheckInClosesAt,
                u.CheckOutOpensAt,
                u.CheckOutClosesAt,
                u.CheckedInAt,
                u.CheckedOutAt,
                u.CheckedOutBy == null
                    ? null
                    : new UsageAccountDTO(u.CheckedOutBy.Id, u.CheckedOutBy.UserName, u.CheckedOutBy.Registration),
                u.Entries
                    .Select(e => new UsageEntryResponseDTO(
                        e.FkScheduleId, e.ClassName, e.Modality, e.NotebooksIn, e.ChargersIn, e.NotebooksOut, e.ChargersOut))
                    .ToList()))
            .AsSplitQuery();
}
