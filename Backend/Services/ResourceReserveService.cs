using System.Security.Claims;
using Backend.Handler;
using Data;
using DTOs.Resource;
using DTOs.ResourceReserve;
using DTOs.Schedule;
using DTOs.Usage;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Reserve;

namespace Backend.Services;

public interface IResourceReserveService
{
    public Task<List<ResourceReserveResponseDTO>> GetResourceReserves(ClaimsPrincipal user, ResourceReserveRequestDTO query);

    public Task<List<ResourceScheduleAvailabilityDTO>> GetAvailability(long resourceId, ResourceAvailabilityRequestDTO query);

    public Task CreateResourceReserve(ClaimsPrincipal user, long resourceId, ResourceReserveCreateRequestDTO data);

    public Task EditResourceReserve(ClaimsPrincipal user, long resourceReserveId, ResourceReserveEditRequestDTO data);

    public Task CancelResourceReserve(ClaimsPrincipal user, long resourceReserveId);

    public Task<List<AdminResourceReserveResponseDTO>> AdminGetResourceReserves(ClaimsPrincipal user, AdminResourceReserveRequestDTO query);

    public Task AdminEditResourceReserve(ClaimsPrincipal user, long resourceReserveId, AdminResourceReserveEditRequestDTO data);
}

/// <summary>Linha projetada de uma reserva de recurso (com a conta, usada só pelo admin).</summary>
internal sealed record ResourceReserveRow(
    long Id,
    DateTimeOffset CreatedAt,
    DateOnly DateFrom,
    DateOnly DateTo,
    string Purpose,
    EReserveStatus Status,
    int Quantity,
    ResourceResponseDTO Resource,
    List<ScheduleResponseDTO> Schedules,
    LinkedSpaceReserveDTO? SpaceReserve,
    AccountRow Account);

public class ResourceReserveService(AppDbContext dbCtx, ICalendarService calendar) : IResourceReserveService
{
    public async Task<List<ResourceReserveResponseDTO>> GetResourceReserves(ClaimsPrincipal user, ResourceReserveRequestDTO query)
    {
        string userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var queryable = dbCtx.ResourceReserves.AsNoTracking().Where(rr => rr.FkAccountId == userId);

        if (query.StartAt.HasValue || query.EndAt.HasValue)
        {
            if (query.StartAt == null || query.EndAt == null)
            {
                throw new BadRequestException("Informe as datas de início e término juntas.");
            }

            ReserveRules.EnsureValidPeriod(query.StartAt.Value, query.EndAt.Value);
            var start = query.StartAt.Value;
            var end = query.EndAt.Value;
            queryable = queryable.Where(rr => rr.DateFrom <= end && rr.DateTo >= start);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchQuery))
        {
            queryable = queryable.Where(ResourceAvailability.ReserveMatchesText(query.SearchQuery));
        }

        if (query.ResourceIds is { Count: > 0 })
        {
            var resourceIds = query.ResourceIds.ToList();
            queryable = queryable.Where(rr => resourceIds.Contains(rr.FkResourceId));
        }

        if (query.ScheduleIds is { Count: > 0 })
        {
            queryable = queryable.Where(rr => rr.ResourceReserveSchedules.Any(s => query.ScheduleIds.Contains(s.FkScheduleId)));
        }

        if (query.Status != null)
        {
            queryable = queryable.Where(rr => rr.Status == query.Status);
        }

        if (query.Standalone != null)
        {
            queryable = query.Standalone.Value
                ? queryable.Where(rr => rr.FkSpaceReserveId == null)
                : queryable.Where(rr => rr.FkSpaceReserveId != null);
        }

        // Limit + 1: o item extra indica ao front que existe próxima página.
        var rows = await Project(queryable
                .OrderBy(rr => rr.DateFrom)
                .ThenBy(rr => rr.Id)
                .Skip(query.Offset)
                .Take(query.Limit + 1))
            .ToListAsync();

        if (rows.Count == 0)
        {
            throw new ResourceNotFoundException("Nenhuma reserva de recurso encontrada com os filtros fornecidos.");
        }

        var usage = await UsageRules.LoadResourceSummaries(
            dbCtx, rows.Select(row => (row.Id, row.SpaceReserve?.Id)).ToList());

        return rows
            .Select(row => new ResourceReserveResponseDTO(
                row.Id, row.CreatedAt, row.DateFrom, row.DateTo, row.Purpose, row.Status, row.Quantity,
                row.Resource, row.Schedules, row.SpaceReserve, usage.GetValueOrDefault(row.Id, UsageSummaryDTO.Empty)))
            .ToList();
    }

    public async Task<List<ResourceScheduleAvailabilityDTO>> GetAvailability(long resourceId, ResourceAvailabilityRequestDTO query)
    {
        ReserveRules.EnsureValidPeriod(query.StartAt, query.EndAt);

        var free = await ResourceAvailability.FreeBySchedule(
            dbCtx,
            resourceId,
            query.StartAt,
            query.EndAt,
            new ReserveExclusion(query.ExcludeSpaceReserveId, query.ExcludeResourceReserveId));

        return free.Select(item => new ResourceScheduleAvailabilityDTO(item.ScheduleId, item.Available)).ToList();
    }

    public async Task CreateResourceReserve(ClaimsPrincipal user, long resourceId, ResourceReserveCreateRequestDTO data)
    {
        string userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        ReserveRules.EnsureValidPeriod(data.StartAt, data.EndAt);
        await calendar.EnsureHasUsableDays(data.StartAt, data.EndAt);
        await ReserveRules.EnsureSchedulesAvailable(dbCtx, data.ScheduleIds);
        await ResourceAvailability.EnsureAvailable(
            dbCtx,
            [new ResourceDemand(resourceId, data.Quantity)],
            data.StartAt,
            data.EndAt,
            data.ScheduleIds,
            ReserveExclusion.None);

        dbCtx.ResourceReserves.Add(new ResourceReserveModel
        {
            FkResourceId = resourceId,
            FkAccountId = userId,
            Purpose = data.Purpose.Trim(),
            Quantity = data.Quantity,
            DateFrom = data.StartAt,
            DateTo = data.EndAt,
            Status = EReserveStatus.Booking,
            ResourceReserveSchedules = data.ScheduleIds
                .Select(scheduleId => new ResourceReserveScheduleModel { FkScheduleId = scheduleId })
                .ToList()
        });

        await dbCtx.SaveChangesAsync();
    }

    public async Task EditResourceReserve(ClaimsPrincipal user, long resourceReserveId, ResourceReserveEditRequestDTO data)
    {
        string userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var reserve = await dbCtx.ResourceReserves
            .Include(rr => rr.ResourceReserveSchedules)
            .FirstOrDefaultAsync(rr => rr.Id == resourceReserveId && rr.FkAccountId == userId);

        if (reserve == null)
        {
            throw new ResourceNotFoundException("Reserva de recurso não encontrada.");
        }

        if (!ResourceAvailability.IsActiveStatus(reserve.Status))
        {
            throw new BadRequestException("Só é possível editar reservas pendentes ou confirmadas.");
        }

        if (data.StartAt == null && data.EndAt == null && data.ScheduleIds == null && data.Quantity == null)
        {
            throw new BadRequestException("Informe ao menos um campo para alterar.");
        }

        await using var transaction = await dbCtx.Database.BeginTransactionAsync();
        await ApplyEdit(reserve, data.StartAt, data.EndAt, data.ScheduleIds, data.Quantity);
        await dbCtx.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task CancelResourceReserve(ClaimsPrincipal user, long resourceReserveId)
    {
        string userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var reserve = await dbCtx.ResourceReserves
            .FirstOrDefaultAsync(rr => rr.Id == resourceReserveId && rr.FkAccountId == userId);

        if (reserve == null)
        {
            throw new ResourceNotFoundException("Reserva de recurso não encontrada.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (reserve.DateTo < today)
        {
            throw new BadRequestException("Não é possível cancelar uma reserva que já terminou.");
        }

        if (!ResourceAvailability.IsActiveStatus(reserve.Status))
        {
            throw new BadRequestException("A reserva já está cancelada ou recusada.");
        }

        // Cancelar o recurso não cancela a sala a que ele está vinculado.
        reserve.Status = EReserveStatus.Canceled;
        await dbCtx.SaveChangesAsync();
    }

    public async Task<List<AdminResourceReserveResponseDTO>> AdminGetResourceReserves(ClaimsPrincipal user, AdminResourceReserveRequestDTO query)
    {
        ReserveRules.EnsureValidPeriod(query.StartAt, query.EndAt);

        string userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        // Admin não gerencia as próprias reservas (mesma regra das reservas de sala).
        var queryable = dbCtx.ResourceReserves.AsNoTracking()
            .Where(rr => rr.FkAccountId != userId)
            .Where(rr => rr.DateFrom <= query.EndAt && rr.DateTo >= query.StartAt);

        if (!string.IsNullOrWhiteSpace(query.SearchQuery))
        {
            queryable = queryable.Where(ResourceAvailability.ReserveMatchesText(query.SearchQuery));
        }

        if (query.ResourceIds is { Count: > 0 })
        {
            var resourceIds = query.ResourceIds.ToList();
            queryable = queryable.Where(rr => resourceIds.Contains(rr.FkResourceId));
        }

        if (query.SpaceIds is { Count: > 0 })
        {
            queryable = queryable.Where(rr => rr.SpaceReserve != null && query.SpaceIds.Contains(rr.SpaceReserve.FkSpaceId));
        }

        if (query.AccountIds is { Count: > 0 })
        {
            queryable = queryable.Where(rr => query.AccountIds.Contains(rr.FkAccountId));
        }

        if (query.Status != null)
        {
            queryable = queryable.Where(rr => rr.Status == query.Status);
        }

        var rows = await Project(queryable
                .OrderBy(rr => rr.DateFrom)
                .ThenBy(rr => rr.Id)
                .Skip(query.Offset)
                .Take(query.Limit + 1))
            .ToListAsync();

        if (rows.Count == 0)
        {
            throw new ResourceNotFoundException("Nenhuma reserva de recurso encontrada com os filtros fornecidos.");
        }

        var accountIds = rows.Select(row => row.Account.Id).Distinct().ToList();
        var roles = await AccountDirectory.LoadRoles(dbCtx, accountIds);
        var subjects = await AccountDirectory.LoadSubjects(dbCtx, accountIds);
        var usage = await UsageRules.LoadResourceSummaries(
            dbCtx, rows.Select(row => (row.Id, row.SpaceReserve?.Id)).ToList());

        return rows
            .Select(row => new AdminResourceReserveResponseDTO(
                row.Id, row.CreatedAt, row.DateFrom, row.DateTo, row.Purpose, row.Status, row.Quantity,
                row.Resource, row.Schedules, row.SpaceReserve, AccountDirectory.ToDto(row.Account, roles, subjects),
                usage.GetValueOrDefault(row.Id, UsageSummaryDTO.Empty)))
            .ToList();
    }

    public async Task AdminEditResourceReserve(ClaimsPrincipal user, long resourceReserveId, AdminResourceReserveEditRequestDTO data)
    {
        string userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var reserve = await dbCtx.ResourceReserves
            .Include(rr => rr.ResourceReserveSchedules)
            .FirstOrDefaultAsync(rr => rr.Id == resourceReserveId && rr.FkAccountId != userId);

        if (reserve == null)
        {
            throw new ResourceNotFoundException("Reserva de recurso não encontrada.");
        }

        bool periodChanged = data.StartAt != null || data.EndAt != null || data.ScheduleIds != null || data.Quantity != null;
        if (data.Status == null && !periodChanged)
        {
            throw new BadRequestException("Informe ao menos um campo para alterar.");
        }

        var wasActive = ResourceAvailability.IsActiveStatus(reserve.Status);
        reserve.Status = data.Status ?? reserve.Status;
        var becomesActive = !wasActive && ResourceAvailability.IsActiveStatus(reserve.Status);

        await using var transaction = await dbCtx.Database.BeginTransactionAsync();

        // Reativar uma reserva volta a ocupar unidades: confere a disponibilidade mesmo sem mudar o período.
        if (periodChanged || becomesActive)
        {
            await ApplyEdit(reserve, data.StartAt, data.EndAt, data.ScheduleIds, data.Quantity);
        }

        await dbCtx.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    /// <summary>
    /// Aplica período, horários e quantidade (nulos não mudam) conferindo a disponibilidade.
    /// Reserva vinculada: período e horários são os da sala, então a mudança vai para a reserva de sala (com checagem
    /// de conflito da sala) e para todos os recursos vinculados a ela. Não salva.
    /// </summary>
    private async Task ApplyEdit(
        ResourceReserveModel reserve, DateOnly? startAt, DateOnly? endAt, HashSet<long>? scheduleIds, int? quantity)
    {
        if ((startAt == null) != (endAt == null))
        {
            throw new BadRequestException("Ambas as datas de início e término devem ser fornecidas para atualização.");
        }

        var targetStart = startAt ?? reserve.DateFrom;
        var targetEnd = endAt ?? reserve.DateTo;
        ReserveRules.EnsureValidPeriod(targetStart, targetEnd);
        if (startAt != null)
        {
            await calendar.EnsureHasUsableDays(targetStart, targetEnd, reserve.CreatedAt);
        }

        var currentScheduleIds = reserve.ResourceReserveSchedules.Select(s => s.FkScheduleId).ToHashSet();
        bool schedulesChanged = scheduleIds is { Count: > 0 };
        var targetScheduleIds = schedulesChanged ? scheduleIds! : currentScheduleIds;

        if (schedulesChanged)
        {
            await ReserveRules.EnsureSchedulesAvailable(
                dbCtx, targetScheduleIds.Where(id => !currentScheduleIds.Contains(id)).ToHashSet());
        }

        var targetQuantity = quantity ?? reserve.Quantity;

        if (reserve.FkSpaceReserveId is { } spaceReserveId)
        {
            var spaceReserve = await dbCtx.SpaceReserves
                .Include(sr => sr.SpaceReserveSchedules)
                .FirstAsync(sr => sr.Id == spaceReserveId);

            if (ResourceAvailability.IsActiveStatus(spaceReserve.Status))
            {
                await ReserveRules.EnsureSpaceNoConflict(
                    dbCtx, spaceReserve.Id, spaceReserve.FkSpaceId, targetStart, targetEnd, targetScheduleIds,
                    spaceReserve.CreatedAt);
            }

            // Os vinculados (inclusive este) passam para o novo período; só a quantidade deste muda.
            await ReserveRules.SyncLinkedResources(
                dbCtx,
                spaceReserve,
                targetStart,
                targetEnd,
                targetScheduleIds,
                requested: null,
                quantityOverrides: new Dictionary<long, int> { [reserve.Id] = targetQuantity });

            spaceReserve.DateFrom = targetStart;
            spaceReserve.DateTo = targetEnd;
            if (schedulesChanged)
            {
                ReserveRules.ReplaceSpaceSchedules(spaceReserve, targetScheduleIds);
            }

            return;
        }

        if (ResourceAvailability.IsActiveStatus(reserve.Status))
        {
            await ResourceAvailability.EnsureAvailable(
                dbCtx,
                [new ResourceDemand(reserve.FkResourceId, targetQuantity)],
                targetStart,
                targetEnd,
                targetScheduleIds,
                new ReserveExclusion(ResourceReserveId: reserve.Id));
        }

        reserve.DateFrom = targetStart;
        reserve.DateTo = targetEnd;
        reserve.Quantity = targetQuantity;
        if (schedulesChanged)
        {
            ReserveRules.ReplaceResourceSchedules(reserve, targetScheduleIds);
        }
    }

    private static IQueryable<ResourceReserveRow> Project(IQueryable<ResourceReserveModel> query) =>
        query
            .Select(rr => new ResourceReserveRow(
                rr.Id,
                rr.CreatedAt,
                rr.DateFrom,
                rr.DateTo,
                rr.Purpose,
                rr.Status,
                rr.Quantity,
                new ResourceResponseDTO(
                    rr.Resource.Id,
                    rr.Resource.Name,
                    rr.Resource.Enabled,
                    rr.Resource.Description,
                    rr.Resource.Reservable,
                    rr.Resource.Quantity,
                    null),
                rr.ResourceReserveSchedules
                    .Select(s => new ScheduleResponseDTO(
                        s.Schedule.Id,
                        s.Schedule.StartAt,
                        s.Schedule.EndAt,
                        s.Schedule.Shift,
                        s.Schedule.Enabled))
                    .ToList(),
                rr.SpaceReserve == null
                    ? null
                    : new LinkedSpaceReserveDTO(
                        rr.SpaceReserve.Id,
                        rr.SpaceReserve.FkSpaceId,
                        rr.SpaceReserve.Space.Name,
                        rr.SpaceReserve.Status),
                new AccountRow(
                    rr.Account.Id,
                    rr.Account.Registration,
                    rr.Account.UserName,
                    rr.Account.Email,
                    rr.Account.LockoutEnd)))
            .AsSplitQuery();
}
