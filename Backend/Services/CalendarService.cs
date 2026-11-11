using System.Security.Claims;
using Backend.Handler;
using Data;
using DTOs.Calendar;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Calendar;
using Models.Usage;

namespace Backend.Services;

/// <summary>
/// Dias que aceitam reservas: fins de semana e feriados ficam bloqueados até um admin liberar a data.
/// Ver <see cref="BlockedDays"/> para a regra aplicada nas reservas.
/// </summary>
public interface ICalendarService
{
    /// <summary>Feriados gerados e liberações do período (gera os feriados do ano se preciso).</summary>
    Task<BlockedDays> Load(DateOnly start, DateOnly end, CancellationToken ct = default);

    /// <summary>
    /// O período precisa ter ao menos um dia que aceite reserva (para uma reserva criada em
    /// <paramref name="reserveCreatedAt"/>; nulo = agora).
    /// </summary>
    Task EnsureHasUsableDays(DateOnly start, DateOnly end, DateTimeOffset? reserveCreatedAt = null);

    Task<List<CalendarDayResponseDTO>> GetDays(CalendarDaysRequestDTO query);

    Task AdminRelease(ClaimsPrincipal user, ReleaseDatesRequestDTO data);
    Task<AffectedReservesResponseDTO> AdminGetAffected(DateOnly date);
    Task AdminRevoke(RevokeDateRequestDTO data);
    Task AdminCreateHoliday(HolidayCreateRequestDTO data);
    Task AdminDeleteHoliday(long holidayId);
}

public class CalendarService(AppDbContext dbCtx, IHolidayProvider holidays) : ICalendarService
{
    private const int MaxRangeDays = 400;

    public async Task<BlockedDays> Load(DateOnly start, DateOnly end, CancellationToken ct = default)
    {
        await holidays.EnsureYears(start, end, ct);
        return await BlockedDays.Load(dbCtx, start, end, ct);
    }

    public async Task EnsureHasUsableDays(DateOnly start, DateOnly end, DateTimeOffset? reserveCreatedAt = null)
    {
        var days = await Load(start, end);
        if (!days.UsableDays(start, end, reserveCreatedAt ?? DateTimeOffset.UtcNow).Any())
        {
            throw new BadRequestException(
                "O período escolhido não tem nenhum dia liberado para reserva (fins de semana e feriados ficam bloqueados).");
        }
    }

    public async Task<List<CalendarDayResponseDTO>> GetDays(CalendarDaysRequestDTO query)
    {
        ReserveRules.EnsureValidPeriod(query.StartAt, query.EndAt);
        if (query.EndAt.DayNumber - query.StartAt.DayNumber + 1 > MaxRangeDays)
        {
            throw new BadRequestException($"Consulte no máximo {MaxRangeDays} dias por vez.");
        }

        var days = await Load(query.StartAt, query.EndAt);
        var holidayRows = await dbCtx.Holidays.AsNoTracking()
            .Where(h => h.Date >= query.StartAt && h.Date <= query.EndAt)
            .OrderBy(h => h.Source)
            .ThenBy(h => h.Name)
            .Select(h => new HolidayResponseDTO(h.Id, h.Date, h.Name, h.Source))
            .ToListAsync();
        var byDate = holidayRows.ToLookup(h => h.Date);

        var result = new List<CalendarDayResponseDTO>();
        for (var day = query.StartAt; day <= query.EndAt; day = day.AddDays(1))
        {
            var restricted = days.IsRestricted(day);
            var released = restricted && days.IsReleased(day);
            result.Add(new CalendarDayResponseDTO(
                day,
                BlockedDays.IsWeekend(day),
                byDate[day].ToList(),
                released,
                restricted && !released));
        }

        return result;
    }

    public async Task AdminRelease(ClaimsPrincipal user, ReleaseDatesRequestDTO data)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var dates = data.Dates.Distinct().ToList();
        var days = await Load(dates.Min(), dates.Max());

        var invalid = dates.Where(d => !days.IsRestricted(d)).ToList();
        if (invalid.Count > 0)
        {
            throw new BadRequestException(
                $"Só fins de semana e feriados precisam ser liberados: {string.Join(", ", invalid.Select(d => d.ToString("dd/MM/yyyy")))}.");
        }

        var existing = await dbCtx.ReleasedDates
            .Where(r => dates.Contains(r.Date))
            .ToDictionaryAsync(r => r.Date);

        var now = DateTimeOffset.UtcNow;
        foreach (var date in dates)
        {
            if (existing.TryGetValue(date, out var release))
            {
                if (release.RevokedAt == null)
                {
                    continue;
                }

                release.RevokedAt = null;
                release.KeepExistingReserves = false;
                release.FkAccountId = userId;
                release.CreatedAt = now;
                continue;
            }

            dbCtx.ReleasedDates.Add(new ReleasedDateModel { Date = date, FkAccountId = userId, CreatedAt = now });
        }

        await dbCtx.SaveChangesAsync();
    }

    public async Task<AffectedReservesResponseDTO> AdminGetAffected(DateOnly date)
    {
        var spaceCount = await dbCtx.SpaceReserves.AsNoTracking()
            .Where(sr => sr.DateFrom <= date && sr.DateTo >= date)
            .Where(ReserveRules.IsSpaceReserveActive)
            .CountAsync();

        // Só as avulsas: as vinculadas acompanham a reserva de sala.
        var resourceCount = await dbCtx.ResourceReserves.AsNoTracking()
            .Where(rr => rr.FkSpaceReserveId == null && rr.DateFrom <= date && rr.DateTo >= date)
            .Where(ResourceAvailability.IsActive)
            .CountAsync();

        return new AffectedReservesResponseDTO(date, spaceCount, resourceCount);
    }

    public async Task AdminRevoke(RevokeDateRequestDTO data)
    {
        var release = await dbCtx.ReleasedDates.FirstOrDefaultAsync(r => r.Date == data.Date && r.RevokedAt == null)
            ?? throw new ResourceNotFoundException("Essa data não está liberada.");

        // Cancelamentos em cascata e remoção de sessões rodam direto no banco: tudo ou nada.
        await using var transaction = await dbCtx.Database.BeginTransactionAsync();

        release.RevokedAt = DateTimeOffset.UtcNow;
        release.KeepExistingReserves = data.KeepReserves;

        if (!data.KeepReserves)
        {
            await DropDayFromReserves(data.Date);
        }

        await dbCtx.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    /// <summary>
    /// Bloquear de novo cancelando: o dia deixa de contar nas reservas que o usavam (pela regra de pular dias
    /// bloqueados). As que ficam sem nenhum outro dia válido são canceladas, e as sessões ainda não iniciadas do dia
    /// somem. Não salva.
    /// </summary>
    private async Task DropDayFromReserves(DateOnly date)
    {
        var spaceReserves = await dbCtx.SpaceReserves
            .Where(sr => sr.DateFrom <= date && sr.DateTo >= date)
            .Where(ReserveRules.IsSpaceReserveActive)
            .ToListAsync();

        var resourceReserves = await dbCtx.ResourceReserves
            .Where(rr => rr.FkSpaceReserveId == null && rr.DateFrom <= date && rr.DateTo >= date)
            .Where(ResourceAvailability.IsActive)
            .ToListAsync();

        if (spaceReserves.Count + resourceReserves.Count == 0)
        {
            return;
        }

        var start = spaceReserves.Select(r => r.DateFrom).Concat(resourceReserves.Select(r => r.DateFrom)).Min();
        var end = spaceReserves.Select(r => r.DateTo).Concat(resourceReserves.Select(r => r.DateTo)).Max();
        // Snapshot ainda com a liberação ativa: o dia revogado é desconsiderado à mão abaixo.
        var days = await Load(start, end);

        bool HasOtherDay(DateOnly from, DateOnly to, DateTimeOffset createdAt) =>
            days.UsableDays(from, to, createdAt).Any(d => d != date);

        foreach (var reserve in spaceReserves.Where(r => !HasOtherDay(r.DateFrom, r.DateTo, r.CreatedAt)))
        {
            reserve.Status = EReserveStatus.Canceled;
            await ReserveRules.CascadeStatus(dbCtx, reserve.Id, EReserveStatus.Canceled);
        }

        foreach (var reserve in resourceReserves.Where(r => !HasOtherDay(r.DateFrom, r.DateTo, r.CreatedAt)))
        {
            reserve.Status = EReserveStatus.Canceled;
        }

        var spaceIds = spaceReserves.Select(r => r.Id).ToList();
        var resourceIds = resourceReserves.Select(r => r.Id).ToList();
        await dbCtx.ReserveUsages
            .Where(u => u.Date == date && u.Status == EUsageStatus.Scheduled)
            .Where(u => (u.FkSpaceReserveId != null && spaceIds.Contains(u.FkSpaceReserveId.Value))
                        || (u.FkResourceReserveId != null && resourceIds.Contains(u.FkResourceReserveId.Value)))
            .ExecuteDeleteAsync();
    }

    public async Task AdminCreateHoliday(HolidayCreateRequestDTO data)
    {
        var name = data.Name.Trim();
        var exists = await dbCtx.Holidays.AnyAsync(h => h.Date == data.Date && h.Name == name);
        if (exists)
        {
            throw new BadRequestException("Já existe um feriado com esse nome nessa data.");
        }

        dbCtx.Holidays.Add(new HolidayModel { Date = data.Date, Name = name, Source = EHolidaySource.Custom });
        await dbCtx.SaveChangesAsync();
    }

    public async Task AdminDeleteHoliday(long holidayId)
    {
        var holiday = await dbCtx.Holidays.FirstOrDefaultAsync(h => h.Id == holidayId)
            ?? throw new ResourceNotFoundException("Feriado não encontrado.");

        if (holiday.Source != EHolidaySource.Custom)
        {
            throw new BadRequestException(
                "Feriados nacionais, estaduais e municipais não podem ser removidos; libere a data no calendário.");
        }

        dbCtx.Holidays.Remove(holiday);
        await dbCtx.SaveChangesAsync();
    }
}
