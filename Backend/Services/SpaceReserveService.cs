using System.Security.Claims;
using Backend.Handler;
using Data;
using DTOs.Account;
using DTOs.Schedule;
using DTOs.Space;
using DTOs.SpaceReserve;
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

public class SpaceReserveService(AppDbContext dbCtx) : ISpaceReserveService
{
    public async Task<List<SpaceReserveResponseDTO>> GetSpaceReserve(ClaimsPrincipal user, SpaceReserveRequestDTO query)
    {
        if (query.StartAt > query.EndAt)
        {
            throw new BadRequestException("Data de inicio precisa ser igual ou anterior a data de término.");
        }

        string userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var queryable = dbCtx.SpaceReserves
            .Include(sr => sr.Space)
            .Include(sr => sr.Account)
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

        var reserves = await queryable
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
                        .Select(s => new SpaceHashDataDTO(s.Id, s.Subject.Name))
                        .ToHashSet(),
                    sr.Space.SpaceResources
                        .Select(r => new SpaceHashDataDTO(r.Id, r.Resource.Name))
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
                    .ToList()
            ))
            .ToListAsync();

        if (reserves.Count == 0)
        {
            throw new ResourceNotFoundException("Nenhuma reserva encontrada para o período informado.");
        }

        return reserves;
    }

    public async Task<List<ScheduleResponseDTO>> GetExistingSchedules(ClaimsPrincipal user, long spaceId, ExistingScheduleRequestDTO query)
    {
        if (query.StartAt > query.EndAt)
        {
            throw new BadRequestException("Data de inicio precisa ser igual ou anterior a data de término.");
        }

        string userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var existingSchedules = await dbCtx.SpaceReserves
            .Where(sr => sr.FkSpaceId == spaceId && sr.DateFrom < query.EndAt && sr.DateTo > query.StartAt && sr.Status == EReserveStatus.Booking && sr.Status == EReserveStatus.Confirmed)
            .SelectMany(sr => sr.SpaceReserveSchedules.Select(s => new ScheduleResponseDTO(
                s.Schedule.Id,
                s.Schedule.StartAt,
                s.Schedule.EndAt,
                s.Schedule.Shift,
                s.Schedule.Enabled
            )))
            .Distinct()
            .ToListAsync();

        if (existingSchedules == null || existingSchedules.Count == 0)
        {
            throw new ResourceNotFoundException("Nenhum horário existente encontrado para o período informado.");
        }

        return existingSchedules;
    }

    public async Task CreateSpaceReserve(ClaimsPrincipal user, long spaceId, SpaceReserveCreateRequestDTO data)
    {
        string userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        if (data.StartAt >= data.EndAt)
        {
            throw new BadRequestException("Data de início precisa ser anterior à data de término.");
        }

        if (data.ScheduleIds != null && data.ScheduleIds.Any())
        {
            bool hasConflict = await dbCtx.SpaceReserves
                .Where(sr => sr.FkSpaceId == spaceId && sr.DateFrom < data.EndAt && sr.DateTo > data.StartAt)
                .AnyAsync(sr => sr.SpaceReserveSchedules.Any(s => data.ScheduleIds.Contains(s.FkScheduleId)));

            if (hasConflict)
            {
                throw new BadRequestException("A reserva conflita com uma reserva existente no mesmo período e horário.");
            }
        }

        var spaceReserve = new SpaceReserveModel
        {
            FkSpaceId = spaceId,
            FkAccountId = userId,
            DateFrom = data.StartAt,
            DateTo = data.EndAt,
            Purpose = data.purpose,
            SpaceReserveSchedules = data.ScheduleIds?.Select(scheduleId => new SpaceReserveScheduleModel
            {
                FkScheduleId = scheduleId
            }).ToList() ?? new List<SpaceReserveScheduleModel>()
        };

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

        bool dataChanged = data.StartAt != null || data.EndAt != null;
        if (dataChanged)
        {
            if (data.StartAt == null || data.EndAt == null)
            {
                throw new BadRequestException("Ambas as datas de início e término devem ser fornecidas para atualização.");
            }
            if (data.StartAt > data.EndAt)
            {
                throw new BadRequestException("Data de início precisa ser igual ou anterior a data de término.");
            }
        }

        var targetStart = data.StartAt ?? spaceReserve.DateFrom;
        var targetEnd = data.EndAt ?? spaceReserve.DateTo;
        var targetScheduleIds = data.ScheduleIds ?? spaceReserve.SpaceReserveSchedules.Select(s => s.FkScheduleId).ToHashSet();

        if (targetScheduleIds.Any())
        {
            bool hasConflict = await dbCtx.SpaceReserves
                .Where(sr => sr.Id != reserveId && sr.DateFrom < targetEnd && sr.DateTo > targetStart)
                .AnyAsync(sr => sr.SpaceReserveSchedules.Any(s => targetScheduleIds.Contains(s.FkScheduleId)));

            if (hasConflict)
            {
                throw new BadRequestException("A reserva conflita com uma reserva existente no mesmo período e horário.");
            }
        }

        if (dataChanged)
        {
            spaceReserve.DateFrom = data.StartAt.Value;
            spaceReserve.DateTo = data.EndAt.Value;
        }

        if (data.ScheduleIds != null && data.ScheduleIds.Count > 0)
        {
            spaceReserve.SpaceReserveSchedules.Clear();
            foreach (var scheduleId in data.ScheduleIds)
            {
                spaceReserve.SpaceReserveSchedules.Add(new SpaceReserveScheduleModel
                {
                    FkSpaceReserveId = spaceReserve.Id,
                    FkScheduleId = scheduleId
                });
            }
        }

        await dbCtx.SaveChangesAsync();
    }

    public async Task CancelSpaceReserve(ClaimsPrincipal user, long reserveId)
    {
        string userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var spaceReserve = await dbCtx.SpaceReserves
            .Where(sr => sr.Id == reserveId && sr.FkAccountId == userId)
            .FirstAsync();
        if (spaceReserve == null)
        {
            throw new ResourceNotFoundException("Reserva não encontrada.");
        }

        spaceReserve.Status = EReserveStatus.Canceled;
        dbCtx.SpaceReserves.Update(spaceReserve);
        await dbCtx.SaveChangesAsync();
    }

    public async Task<List<AdminSpaceReserveResponseDTO>> AdminGetSpaceReserve(ClaimsPrincipal user, AdminSpaceReserveRequestDTO query)
    {
        if (query.StartAt > query.EndAt)
        {
            throw new BadRequestException("Data de inicio precisa ser igual ou anterior a data de término.");
        }

        string userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var queryable = dbCtx.SpaceReserves
            .Include(sr => sr.Space)
            .Include(sr => sr.Account)
            .Where(sr => sr.FkAccountId != userId)
            .Where(sr => sr.DateFrom >= query.StartAt && sr.DateTo <= query.EndAt);

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

        var reserves = await queryable
            .Select(sr => new AdminSpaceReserveResponseDTO(
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
                        .Select(s => new SpaceHashDataDTO(s.Id, s.Subject.Name))
                        .ToHashSet(),
                    sr.Space.SpaceResources
                        .Select(r => new SpaceHashDataDTO(r.Id, r.Resource.Name))
                        .ToHashSet()
                ),
                new AccountResponseDTO(
                    sr.Account.Id,
                    sr.Account.Registration,
                    sr.Account.UserName,
                    sr.Account.Email,
                    dbCtx.UserRoles
                        .Join(
                            dbCtx.Roles,
                            userRole => userRole.RoleId,
                            role => role.Id,
                            (userRole, role) => new { userRole.UserId, role.Name })
                        .Any(role => role.UserId == sr.Account.Id && role.Name == nameof(EAccountRole.Owner))
                        ? EAccountRole.Owner
                        : dbCtx.UserRoles
                            .Join(
                                dbCtx.Roles,
                                userRole => userRole.RoleId,
                                role => role.Id,
                                (userRole, role) => new { userRole.UserId, role.Name })
                            .Any(role => role.UserId == sr.Account.Id && role.Name == nameof(EAccountRole.Admin))
                            ? EAccountRole.Admin
                            : EAccountRole.User,
                    sr.Account.LockoutEnd == null
                ),
                sr.SpaceReserveSchedules
                    .Select(sc => new ScheduleResponseDTO(
                        sc.Schedule.Id,
                        sc.Schedule.StartAt,
                        sc.Schedule.EndAt,
                        sc.Schedule.Shift,
                        sc.Schedule.Enabled
                    ))
                    .ToList()
            ))
            .ToListAsync();

        if (reserves.Count == 0)
        {
            throw new ResourceNotFoundException("Nenhuma reserva encontrada para o período informado.");
        }

        return reserves;
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

        spaceReserve.Status = data.Status;
        await dbCtx.SaveChangesAsync();
    }
}