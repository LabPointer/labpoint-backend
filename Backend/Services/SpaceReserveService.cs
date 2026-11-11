using System.Security.Claims;
using Backend.Handler;
using Data;
using DTOs.Account;
using DTOs.Schedule;
using DTOs.Space;
using DTOs.SpaceReserve;
using DTOs.Usage;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Reserve;

namespace Backend.Services;

public interface ISpaceReserveService
{
    public Task<List<SpaceReserveResponseDTO>> GetSpaceReserve(ClaimsPrincipal user, SpaceReserveRequestDTO query);

    public Task<List<ScheduleResponseDTO>> GetExistingSchedules(ClaimsPrincipal user, long spaceId, ExistingScheduleRequestDTO query);

    public Task CreateSpaceReserve(ClaimsPrincipal user, long spaceId, SpaceReserveCreateRequestDTO data);

    public Task EditSpaceReserve(ClaimsPrincipal user, long reserveId, SpaceReserveEditRequestDTO data);

    public Task CancelSpaceReserve(ClaimsPrincipal user, long reserveId);

    public Task<List<AdminSpaceReserveResponseDTO>> AdminGetSpaceReserve(ClaimsPrincipal user, AdminSpaceReserveRequestDTO query);

    public Task AdminEditSpaceReserve(ClaimsPrincipal user, long reserveId, AdminSpaceReserveEditRequestDTO data);
}

public class SpaceReserveService(AppDbContext dbCtx, ICalendarService calendar) : ISpaceReserveService
{
    public async Task<List<SpaceReserveResponseDTO>> GetSpaceReserve(ClaimsPrincipal user, SpaceReserveRequestDTO query)
    {
        if (query.StartAt > query.EndAt)
        {
            throw new BadRequestException("Data de inicio precisa ser igual ou anterior a data de término.");
        }

        string userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var queryable = dbCtx.SpaceReserves
            .Where(sr => sr.FkAccountId == userId)
            .Where(sr => sr.DateFrom >= query.StartAt && sr.DateTo <= query.EndAt);

        if (query.SpaceIds != null && query.SpaceIds.Count > 0)
        {
            queryable = queryable.Where(sr => query.SpaceIds.Contains(sr.FkSpaceId));
        }

        if (query.Status != null)
        {
            queryable = queryable.Where(sr => sr.Status == query.Status);
        }

        // Várias coleções na mesma projeção (matérias, recursos, horários): consulta dividida evita o
        // produto cartesiano entre elas. Os Includes foram removidos: não têm efeito com Select.
        var reserves = await queryable
            .OrderBy(sr => sr.DateFrom)
            .ThenBy(sr => sr.Id)
            .Select(sr => new SpaceReserveResponseDTO(
                sr.Id,
                sr.CreatedAt,
                sr.DateFrom,
                sr.DateTo,
                sr.Purpose,
                sr.Status,
                new SpaceResponseDTO(
                    sr.Space.Id,
                    sr.Space.Name,
                    sr.Space.Capacity,
                    sr.Space.Description,
                    sr.Space.Locked,
                    sr.Space.SpaceSubjects
                        .Select(s => new SpaceHashDataDTO(s.FkSubjectId, s.Subject.Name))
                        .ToHashSet(),
                    sr.Space.SpaceResources
                        .Select(r => new SpaceHashDataDTO(r.FkResourceId, r.Resource.Name))
                        .ToHashSet()
                ),
                sr.SpaceReserveSchedules
                    .Select(sc => new ScheduleResponseDTO(
                        sc.Schedule.Id,
                        sc.Schedule.StartAt,
                        sc.Schedule.EndAt,
                        sc.Schedule.Shift,
                        sc.Schedule.Enabled
                    ))
                    .ToList(),
                sr.ResourceReserves
                    .OrderBy(rr => rr.Resource.Name)
                    .Select(rr => new LinkedResourceDTO(
                        rr.Id, rr.FkResourceId, rr.Resource.Name, rr.Resource.Description, rr.Quantity, rr.Status))
                    .ToList()
            ))
            .AsSplitQuery()
            .ToListAsync();

        if (reserves.Count == 0)
        {
            throw new ResourceNotFoundException("Nenhuma reserva encontrada para o período informado.");
        }

        var usage = await UsageRules.LoadSpaceSummaries(dbCtx, reserves.Select(r => r.Id).ToList());
        return reserves
            .Select(r => r with { Usage = usage.GetValueOrDefault(r.Id, UsageSummaryDTO.Empty) })
            .ToList();
    }

    public async Task<List<ScheduleResponseDTO>> GetExistingSchedules(ClaimsPrincipal user, long spaceId, ExistingScheduleRequestDTO query)
    {
        if (query.StartAt > query.EndAt)
        {
            throw new BadRequestException("Data de inicio precisa ser igual ou anterior a data de término.");
        }

        var reserves = await dbCtx.SpaceReserves.AsNoTracking()
            .Where(sr => sr.FkSpaceId == spaceId && sr.DateFrom <= query.EndAt && sr.DateTo >= query.StartAt)
            .Where(ReserveRules.IsSpaceReserveActive)
            .Select(sr => new
            {
                sr.Id,
                sr.DateFrom,
                sr.DateTo,
                sr.CreatedAt,
                Schedules = sr.SpaceReserveSchedules.Select(s => new ScheduleResponseDTO(
                    s.Schedule.Id,
                    s.Schedule.StartAt,
                    s.Schedule.EndAt,
                    s.Schedule.Shift,
                    s.Schedule.Enabled
                )).ToList()
            })
            .AsSplitQuery()
            .ToListAsync();

        // Um horário só está ocupado se, em algum dia do período, o turno dele não ficou ausente (sem check-in).
        var absent = await UsageRules.LoadAbsentSpace(dbCtx, query.StartAt, query.EndAt);
        // Dias bloqueados (fim de semana ou feriado sem liberação) não ocupam nada.
        var blocked = await calendar.Load(query.StartAt, query.EndAt);
        var now = DateTimeOffset.UtcNow;
        var existingSchedules = reserves
            .SelectMany(sr =>
            {
                var from = sr.DateFrom > query.StartAt ? sr.DateFrom : query.StartAt;
                var to = sr.DateTo < query.EndAt ? sr.DateTo : query.EndAt;
                return sr.Schedules.Where(s =>
                {
                    for (var day = from; day <= to; day = day.AddDays(1))
                    {
                        if (!blocked.IsUsable(day, now) || !blocked.IsUsable(day, sr.CreatedAt))
                        {
                            continue;
                        }

                        if (!absent.Contains((sr.Id, day, s.Shift)))
                        {
                            return true;
                        }
                    }

                    return false;
                });
            })
            .DistinctBy(s => s.Id)
            .ToList();

        if (existingSchedules.Count == 0)
        {
            throw new ResourceNotFoundException("Nenhum horário existente encontrado para o período informado.");
        }

        return existingSchedules;
    }

    public async Task CreateSpaceReserve(ClaimsPrincipal user, long spaceId, SpaceReserveCreateRequestDTO data)
    {
        string userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        ReserveRules.EnsureValidPeriod(data.StartAt, data.EndAt);
        await calendar.EnsureHasUsableDays(data.StartAt, data.EndAt);

        // Regras de negócio no servidor (o front só esconde espaços bloqueados): sem isso, chamar a API direto
        // reservava espaços bloqueados e IDs inexistentes terminavam em erro 500 de chave estrangeira.
        var space = await dbCtx.Spaces.AsNoTracking()
            .Where(s => s.Id == spaceId)
            .Select(s => new { s.Locked })
            .FirstOrDefaultAsync();

        if (space == null)
        {
            throw new ResourceNotFoundException("Espaço não encontrado.");
        }

        if (space.Locked)
        {
            throw new BadRequestException("O espaço está bloqueado e não aceita novas reservas.");
        }

        await ReserveRules.EnsureSchedulesAvailable(dbCtx, data.ScheduleIds);
        await ReserveRules.EnsureSpaceNoConflict(
            dbCtx, 0, spaceId, data.StartAt, data.EndAt, data.ScheduleIds, DateTimeOffset.UtcNow);

        var hasElevatedRole = user.IsInRole("Root") || user.IsInRole("Admin");
        if (data.Lock && !hasElevatedRole)
        {
            throw new BadRequestException("Você não tem permissão para bloquear a reserva de um espaço.");
        }
        var status = data.Lock && hasElevatedRole ? EReserveStatus.Locked : EReserveStatus.Booking;
        var spaceReserve = new SpaceReserveModel
        {
            FkSpaceId = spaceId,
            FkAccountId = userId,
            DateFrom = data.StartAt,
            DateTo = data.EndAt,
            Purpose = data.purpose,
            Status = status,
            SpaceReserveSchedules = data.ScheduleIds.Select(scheduleId => new SpaceReserveScheduleModel
            {
                FkScheduleId = scheduleId
            }).ToList()
        };

        // Recursos pedidos junto: conferidos e criados como reservas vinculadas (mesmo período e horários).
        if (data.Resources is { Count: > 0 })
        {
            await ReserveRules.SyncLinkedResources(
                dbCtx, spaceReserve, data.StartAt, data.EndAt, data.ScheduleIds, data.Resources);
        }

        dbCtx.SpaceReserves.Add(spaceReserve);
        await dbCtx.SaveChangesAsync();
    }

    public async Task EditSpaceReserve(ClaimsPrincipal user, long reserveId, SpaceReserveEditRequestDTO data)
    {
        string userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var spaceReserve = await dbCtx.SpaceReserves
            .Include(sr => sr.SpaceReserveSchedules)
            .FirstOrDefaultAsync(sr => sr.Id == reserveId && sr.FkAccountId == userId);

        if (spaceReserve == null)
        {
            throw new ResourceNotFoundException("Reserva não encontrada.");
        }

        if ((spaceReserve.Status == EReserveStatus.Canceled || spaceReserve.Status == EReserveStatus.Rejected) && !(user.IsInRole("Root") || user.IsInRole("Admin")))
        {
            throw new BadRequestException("Não é possível editar uma reserva cancelada ou rejeitada.");
        }

        await ApplyPeriodAndResources(spaceReserve, data.StartAt, data.EndAt, data.ScheduleIds, data.Resources);
        await dbCtx.SaveChangesAsync();
    }

    public async Task CancelSpaceReserve(ClaimsPrincipal user, long reserveId)
    {
        string userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        await using var transaction = await dbCtx.Database.BeginTransactionAsync();

        var affected = await dbCtx.SpaceReserves
            .Where(sr => sr.Id == reserveId && sr.FkAccountId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(sr => sr.Status, EReserveStatus.Canceled));

        if (affected == 0)
        {
            throw new ResourceNotFoundException("Reserva não encontrada.");
        }

        // Cancelar a sala cancela os recursos reservados junto.
        await ReserveRules.CascadeStatus(dbCtx, reserveId, EReserveStatus.Canceled);
        await transaction.CommitAsync();
    }

    public async Task<List<AdminSpaceReserveResponseDTO>> AdminGetSpaceReserve(ClaimsPrincipal user, AdminSpaceReserveRequestDTO query)
    {
        if (query.StartAt > query.EndAt)
        {
            throw new BadRequestException("Data de inicio precisa ser igual ou anterior a data de término.");
        }

        string userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var queryable = dbCtx.SpaceReserves
            .Where(sr => sr.FkAccountId != userId)
            // Admin: reservas que SOBREPÕEM o período (e não só as totalmente contidas nele)
            .Where(sr => sr.DateFrom <= query.EndAt && sr.DateTo >= query.StartAt);

        if (!string.IsNullOrWhiteSpace(query.SearchQuery))
        {
            var pattern = QueryHelpers.ContainsPattern(query.SearchQuery);
            queryable = queryable.Where(sr => EF.Functions.ILike(sr.Space.Name, pattern));
        }

        // Antes o filtro de horários era recebido mas ignorado.
        if (query.ScheduleIds != null && query.ScheduleIds.Count > 0)
        {
            queryable = queryable.Where(sr => sr.SpaceReserveSchedules.Any(s => query.ScheduleIds.Contains(s.FkScheduleId)));
        }

        if (query.AccountIds != null && query.AccountIds.Count > 0)
        {
            queryable = queryable.Where(sr => query.AccountIds.Contains(sr.FkAccountId));
        }

        if (query.SpaceIds != null && query.SpaceIds.Count > 0)
        {
            queryable = queryable.Where(sr => query.SpaceIds.Contains(sr.FkSpaceId));
        }

        if (query.Status != null)
        {
            queryable = queryable.Where(sr => sr.Status == query.Status);
        }

        var rows = await queryable
            .OrderBy(sr => sr.DateFrom)
            .ThenBy(sr => sr.Id)
            .Select(sr => new
            {
                sr.Id,
                sr.CreatedAt,
                sr.DateFrom,
                sr.DateTo,
                sr.Purpose,
                sr.Status,
                Space = new SpaceResponseDTO(
                    sr.Space.Id,
                    sr.Space.Name,
                    sr.Space.Capacity,
                    sr.Space.Description,
                    sr.Space.Locked,
                    sr.Space.SpaceSubjects
                        .Select(s => new SpaceHashDataDTO(s.FkSubjectId, s.Subject.Name))
                        .ToHashSet(),
                    sr.Space.SpaceResources
                        .Select(r => new SpaceHashDataDTO(r.FkResourceId, r.Resource.Name))
                        .ToHashSet()
                ),
                Account = new AccountRow(
                    sr.Account.Id,
                    sr.Account.Registration,
                    sr.Account.UserName,
                    sr.Account.Email,
                    sr.Account.LockoutEnd),
                Schedules = sr.SpaceReserveSchedules
                    .Select(sc => new ScheduleResponseDTO(
                        sc.Schedule.Id,
                        sc.Schedule.StartAt,
                        sc.Schedule.EndAt,
                        sc.Schedule.Shift,
                        sc.Schedule.Enabled
                    ))
                    .ToList(),
                Resources = sr.ResourceReserves
                    .OrderBy(rr => rr.Resource.Name)
                    .Select(rr => new LinkedResourceDTO(
                        rr.Id, rr.FkResourceId, rr.Resource.Name, rr.Resource.Description, rr.Quantity, rr.Status))
                    .ToList()
            })
            .AsSplitQuery()
            .ToListAsync();

        if (rows.Count == 0)
        {
            throw new ResourceNotFoundException("Nenhuma reserva encontrada para o período informado.");
        }

        // Cargo e matéria dos usuários em lote. Antes eram 2 EXISTS + 1 subconsulta correlacionados POR LINHA.
        var accountIds = rows.Select(row => row.Account.Id).Distinct().ToList();
        var roles = await AccountDirectory.LoadRoles(dbCtx, accountIds);
        var subjects = await AccountDirectory.LoadSubjects(dbCtx, accountIds);
        var usage = await UsageRules.LoadSpaceSummaries(dbCtx, rows.Select(row => row.Id).ToList());

        return rows
            .Select(row => new AdminSpaceReserveResponseDTO(
                row.Id,
                row.CreatedAt,
                row.DateFrom,
                row.DateTo,
                row.Purpose,
                row.Status,
                row.Space,
                AccountDirectory.ToDto(row.Account, roles, subjects),
                row.Schedules,
                row.Resources,
                usage.GetValueOrDefault(row.Id, UsageSummaryDTO.Empty)))
            .ToList();
    }

    public async Task AdminEditSpaceReserve(ClaimsPrincipal user, long reserveId, AdminSpaceReserveEditRequestDTO data)
    {
        string userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var spaceReserve = await dbCtx.SpaceReserves
            .Include(sr => sr.SpaceReserveSchedules)
            .FirstOrDefaultAsync(sr => sr.Id == reserveId && sr.FkAccountId != userId);

        if (spaceReserve == null)
        {
            throw new ResourceNotFoundException("Reserva não encontrada.");
        }

        bool datesChanged = data.StartAt != null || data.EndAt != null;
        bool schedulesChanged = data.ScheduleIds != null && data.ScheduleIds.Count > 0;

        if (data.Status == null && !datesChanged && !schedulesChanged && data.Resources == null)
        {
            throw new BadRequestException("Informe ao menos um campo para alterar.");
        }

        var previousStatus = spaceReserve.Status;
        var targetStatus = data.Status ?? spaceReserve.Status;

        await using var transaction = await dbCtx.Database.BeginTransactionAsync();

        // O status vem antes: a disponibilidade só é conferida se a reserva ficar (ou continuar) ativa.
        spaceReserve.Status = targetStatus;
        await ApplyPeriodAndResources(spaceReserve, data.StartAt, data.EndAt, data.ScheduleIds, data.Resources);
        await dbCtx.SaveChangesAsync();

        if (targetStatus != previousStatus)
        {
            await ReserveRules.CascadeStatus(dbCtx, spaceReserve.Id, targetStatus);
        }

        await transaction.CommitAsync();
    }

    /// <summary>
    /// Valida e aplica período, horários e recursos vinculados (campos nulos não mudam). A sala só é conferida
    /// contra conflitos se a reserva estiver ativa; os recursos vinculados seguem o mesmo período e horários.
    /// Não salva.
    /// </summary>
    private async Task ApplyPeriodAndResources(
        SpaceReserveModel spaceReserve,
        DateOnly? startAt,
        DateOnly? endAt,
        HashSet<long>? scheduleIds,
        List<ReserveResourceItemDTO>? resources)
    {
        bool datesChanged = startAt != null || endAt != null;
        if (datesChanged)
        {
            if (startAt == null || endAt == null)
            {
                throw new BadRequestException("Ambas as datas de início e término devem ser fornecidas para atualização.");
            }

            ReserveRules.EnsureValidPeriod(startAt.Value, endAt.Value);
            await calendar.EnsureHasUsableDays(startAt.Value, endAt.Value, spaceReserve.CreatedAt);
        }

        bool schedulesChanged = scheduleIds != null && scheduleIds.Count > 0;
        var currentScheduleIds = spaceReserve.SpaceReserveSchedules.Select(s => s.FkScheduleId).ToHashSet();

        if (schedulesChanged)
        {
            await ReserveRules.EnsureSchedulesAvailable(
                dbCtx, scheduleIds!.Where(id => !currentScheduleIds.Contains(id)).ToHashSet());
        }

        var targetStart = startAt ?? spaceReserve.DateFrom;
        var targetEnd = endAt ?? spaceReserve.DateTo;
        var targetScheduleIds = schedulesChanged ? scheduleIds! : currentScheduleIds;

        if (ResourceAvailability.IsActiveStatus(spaceReserve.Status) && targetScheduleIds.Count > 0)
        {
            await ReserveRules.EnsureSpaceNoConflict(
                dbCtx, spaceReserve.Id, spaceReserve.FkSpaceId, targetStart, targetEnd, targetScheduleIds,
                spaceReserve.CreatedAt);
        }

        if (datesChanged || schedulesChanged || resources != null)
        {
            await ReserveRules.SyncLinkedResources(
                dbCtx, spaceReserve, targetStart, targetEnd, targetScheduleIds, resources);
        }

        if (datesChanged)
        {
            spaceReserve.DateFrom = targetStart;
            spaceReserve.DateTo = targetEnd;
        }

        if (schedulesChanged)
        {
            ReserveRules.ReplaceSpaceSchedules(spaceReserve, targetScheduleIds);
        }
    }
}
