using Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Fins de semana e feriados de um período, com as liberações feitas pelos admins. Por padrão esses dias não aceitam
/// reservas: uma reserva de vários dias que passa por eles é aceita, mas os dias bloqueados não contam (sem sessão de
/// uso, sem ocupar sala ou recurso). Só lê o banco; os feriados do ano precisam ter sido gerados antes
/// (<see cref="IHolidayProvider.EnsureYears"/>).
/// </summary>
public sealed class BlockedDays
{
    private readonly HashSet<DateOnly> _holidays;
    private readonly Dictionary<DateOnly, Release> _releases;

    private sealed record Release(DateTimeOffset? RevokedAt, bool KeepExistingReserves);

    private BlockedDays(HashSet<DateOnly> holidays, Dictionary<DateOnly, Release> releases)
    {
        _holidays = holidays;
        _releases = releases;
    }

    /// <summary>Nada bloqueado (para testes ou quando o período é vazio).</summary>
    public static readonly BlockedDays None = new([], []);

    public static async Task<BlockedDays> Load(AppDbContext dbCtx, DateOnly start, DateOnly end, CancellationToken ct = default)
    {
        if (start > end)
        {
            return None;
        }

        var holidays = await dbCtx.Holidays.AsNoTracking()
            .Where(h => h.Date >= start && h.Date <= end)
            .Select(h => h.Date)
            .Distinct()
            .ToListAsync(ct);

        var releases = await dbCtx.ReleasedDates.AsNoTracking()
            .Where(r => r.Date >= start && r.Date <= end)
            .Select(r => new { r.Date, r.RevokedAt, r.KeepExistingReserves })
            .ToListAsync(ct);

        return new BlockedDays(
            holidays.ToHashSet(),
            releases.ToDictionary(r => r.Date, r => new Release(r.RevokedAt, r.KeepExistingReserves)));
    }

    public static bool IsWeekend(DateOnly day) => day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    public bool IsHoliday(DateOnly day) => _holidays.Contains(day);

    /// <summary>Fim de semana ou feriado: bloqueado, a menos que liberado.</summary>
    public bool IsRestricted(DateOnly day) => IsWeekend(day) || IsHoliday(day);

    /// <summary>Liberação ativa (não revogada).</summary>
    public bool IsReleased(DateOnly day) => _releases.TryGetValue(day, out var release) && release.RevokedAt == null;

    /// <summary>
    /// O dia vale para uma reserva criada em <paramref name="reserveCreatedAt"/>: dia útil, liberado, ou liberação
    /// revogada com "manter reservas" e a reserva é anterior à revogação.
    /// </summary>
    public bool IsUsable(DateOnly day, DateTimeOffset reserveCreatedAt)
    {
        if (!IsRestricted(day))
        {
            return true;
        }

        if (!_releases.TryGetValue(day, out var release))
        {
            return false;
        }

        return release.RevokedAt == null
               || (release.KeepExistingReserves && reserveCreatedAt < release.RevokedAt.Value);
    }

    /// <summary>Usável para uma reserva nova (criada agora).</summary>
    public bool IsUsableForNew(DateOnly day) => IsUsable(day, DateTimeOffset.UtcNow);

    public IEnumerable<DateOnly> UsableDays(DateOnly start, DateOnly end, DateTimeOffset reserveCreatedAt)
    {
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            if (IsUsable(day, reserveCreatedAt))
            {
                yield return day;
            }
        }
    }
}
