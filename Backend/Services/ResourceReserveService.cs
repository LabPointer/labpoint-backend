using System.Security.Claims;
using Backend.Handler;
using Data;
using DTOs.ResourceReserve;
using DTOs.Schedule;
using DTOs.Space;
using DTOs.SpaceReserve;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Reserve;

namespace Backend.Services;

public interface IResourceReserveService
{
    public Task<IEnumerable<ResourceReserveResponseDTO>> GetResourceReserves(ClaimsPrincipal user, ResourceReserveRequestDTO query);

    public Task CreateResourceReserve(ClaimsPrincipal user, long spaceReserveId, ResourceReserveCreateRequestDTO data);

    public Task EditResourceReserve(ClaimsPrincipal user, long resourceReserveId, ResourceReserveEditRequestDTO data);

    public Task CancelResourceReserve(ClaimsPrincipal user, long resourceReserveId);
}

public class ResourceReserveService(AppDbContext dbCtx) : IResourceReserveService
{
    public async Task<IEnumerable<ResourceReserveResponseDTO>> GetResourceReserves(ClaimsPrincipal user, ResourceReserveRequestDTO query)
    {
        string userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var queryable = dbCtx.ResourceReserves.AsQueryable();

        queryable = queryable.Where(r => r.FkAccountId == userId);

        if (!string.IsNullOrEmpty(query.SearchQuery))
        {
            queryable = queryable.Where(r => EF.Functions
                .ToTsVector("portuguese", r.Resource.Name)
                .Matches(EF.Functions.PhraseToTsQuery("portuguese", query.SearchQuery)));
        }

        if (query.SpaceIds != null)
        {
            queryable = queryable.Where(r => query.SpaceIds.Contains(r.SpaceReserve.FkSpaceId));
        }

        var resourceReservesRes = await queryable
            .Take(query.Limit)
            .Skip(query.Offset)
            .ToListAsync();

        if (resourceReservesRes.Count == 0)
        {
            throw new ResourceNotFoundException("Nenhuma reserva de recurso encontrada com os filtros fornecidos.");
        }

        var resourceReserves = resourceReservesRes.Select(r => new ResourceReserveResponseDTO(
            r.Id,
            r.Resource.Name,
            new SpaceReserveResponseDTO(
                r.SpaceReserve.Id,
                r.SpaceReserve.CreatedAt,
                r.SpaceReserve.DateFrom,
                r.SpaceReserve.DateTo,
                r.SpaceReserve.Purpose,
                r.SpaceReserve.Status,
                new SpaceResponseDTO(
                    r.SpaceReserve.Space.Id,
                    r.SpaceReserve.Space.Name,
                    r.SpaceReserve.Space.Capacity,
                    r.SpaceReserve.Space.Description,
                    r.SpaceReserve.Space.Locked,
                    r.SpaceReserve.Space.SpaceSubjects.Select(s => new SpaceHashDataDTO(
                        s.Subject.Id,
                        s.Subject.Name
                    )).ToHashSet(),
                    r.SpaceReserve.Space.SpaceResources.Select(r => new SpaceHashDataDTO(
                        r.Resource.Id,
                        r.Resource.Name
                    )).ToHashSet()
                ),
                r.SpaceReserve.SpaceReserveSchedules.Select(srs => new ScheduleResponseDTO(
                    srs.Id,
                    srs.Schedule.StartAt,
                    srs.Schedule.EndAt,
                    srs.Schedule.Shift,
                    srs.Schedule.Enabled
                )).ToList()
            )
        ));
        
        return resourceReserves;
    }

    public async Task CreateResourceReserve(ClaimsPrincipal user, long spaceReserveId, ResourceReserveCreateRequestDTO data)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedException("Usuário não identificado.", true);
        }

        var spaceReserve = await dbCtx.SpaceReserves
            .Include(reserve => reserve.SpaceReserveSchedules)
            .FirstOrDefaultAsync(reserve => reserve.Id == spaceReserveId && reserve.FkAccountId == userId);

        if (spaceReserve == null)
        {
            throw new BadRequestException("A reserva de espaço informada não existe.");
        }

        if (spaceReserve.Status is EReserveStatus.Confirmed or EReserveStatus.Canceled or EReserveStatus.Rejected)
        {
            throw new BadRequestException("A reserva de espaço não permite reservas de recursos neste status.");
        }

        var resourceBelongsToSpace = await dbCtx.SpaceResources
            .AnyAsync(spaceResource =>
                spaceResource.FkSpaceId == spaceReserve.FkSpaceId &&
                spaceResource.FkResourceId == data.ResourceId &&
                spaceResource.Resource.Enabled &&
                spaceResource.Resource.CanReserve);

        if (!resourceBelongsToSpace)
        {
            throw new BadRequestException("O recurso informado não está disponível para o espaço da reserva.");
        }

        var scheduleIds = spaceReserve.SpaceReserveSchedules
            .Select(schedule => schedule.FkScheduleId)
            .ToHashSet();

        var hasConflict = await dbCtx.ResourceReserves
            .Where(resourceReserve => resourceReserve.FkResourceId == data.ResourceId)
            .Where(resourceReserve => resourceReserve.SpaceReserve.Status != EReserveStatus.Canceled &&
                                      resourceReserve.SpaceReserve.Status != EReserveStatus.Rejected)
            .Where(resourceReserve =>
                resourceReserve.SpaceReserve.DateFrom <= spaceReserve.DateTo &&
                resourceReserve.SpaceReserve.DateTo >= spaceReserve.DateFrom)
            .AnyAsync(resourceReserve => resourceReserve.SpaceReserve.SpaceReserveSchedules
                .Any(schedule => scheduleIds.Contains(schedule.FkScheduleId)));

        if (hasConflict)
        {
            throw new BadRequestException("O recurso já está reservado para um horário conflitante.");
        }

        var resourceExists = await dbCtx.Resources
            .AnyAsync(resource => resource.Id == data.ResourceId && resource.Enabled && resource.CanReserve);
        
        if (!resourceExists)
        {
            throw new BadRequestException("O recurso informado não existe ou não está disponível para reserva.");
        }

        dbCtx.ResourceReserves.Add(new ResourceReserveModel
        {
            FkResourceId = data.ResourceId,
            FkSpaceReserveId = spaceReserve.Id,
            FkAccountId = userId,
            Purpose = data.Purpose
        });

        await dbCtx.SaveChangesAsync();
    }

    public async Task EditResourceReserve(ClaimsPrincipal user, long resourceReserveId, ResourceReserveEditRequestDTO data)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedException("Usuário não identificado.", true);
        }

        var resourceReserve = await dbCtx.ResourceReserves
            .Include(reserve => reserve.SpaceReserve)
                .ThenInclude(spaceReserve => spaceReserve.SpaceReserveSchedules)
            .FirstOrDefaultAsync(reserve => reserve.Id == resourceReserveId && reserve.FkAccountId == userId);

        if (resourceReserve == null)
        {
            throw new BadRequestException("A reserva de recurso informada não existe.");
        }

        var targetSpaceReserve = await dbCtx.SpaceReserves
            .Include(reserve => reserve.SpaceReserveSchedules)
            .FirstOrDefaultAsync(reserve => reserve.Id == data.SpaceReserveId && reserve.FkAccountId == userId);

        if (targetSpaceReserve == null)
        {
            throw new BadRequestException("A nova reserva de espaço informada não existe.");
        }

        if (targetSpaceReserve.Status is EReserveStatus.Confirmed or EReserveStatus.Canceled or EReserveStatus.Rejected)
        {
            throw new BadRequestException("A nova reserva de espaço não permite reservas de recursos neste status.");
        }

        var resourceBelongsToSpace = await dbCtx.SpaceResources
            .AnyAsync(spaceResource =>
                spaceResource.FkSpaceId == targetSpaceReserve.FkSpaceId &&
                spaceResource.FkResourceId == resourceReserve.FkResourceId &&
                spaceResource.Resource.Enabled &&
                spaceResource.Resource.CanReserve);

        if (!resourceBelongsToSpace)
        {
            throw new BadRequestException("O recurso não está associado ao novo espaço da reserva.");
        }

        var scheduleIds = targetSpaceReserve.SpaceReserveSchedules
            .Select(schedule => schedule.FkScheduleId)
            .ToHashSet();

        var hasConflict = await dbCtx.ResourceReserves
            .Where(otherReserve =>
                otherReserve.Id != resourceReserveId &&
                otherReserve.FkResourceId == resourceReserve.FkResourceId)
            .Where(otherReserve => otherReserve.SpaceReserve.Status != EReserveStatus.Canceled &&
                                   otherReserve.SpaceReserve.Status != EReserveStatus.Rejected)
            .Where(otherReserve =>
                otherReserve.SpaceReserve.DateFrom <= targetSpaceReserve.DateTo &&
                otherReserve.SpaceReserve.DateTo >= targetSpaceReserve.DateFrom)
            .AnyAsync(otherReserve => otherReserve.SpaceReserve.SpaceReserveSchedules
                .Any(schedule => scheduleIds.Contains(schedule.FkScheduleId)));

        if (hasConflict)
        {
            throw new BadRequestException("O recurso já está reservado para um horário conflitante.");
        }

        resourceReserve.FkSpaceReserveId = targetSpaceReserve.Id;
        resourceReserve.Purpose = data.Purpose ?? resourceReserve.Purpose;

        await dbCtx.SaveChangesAsync();
    }

    public async Task CancelResourceReserve(ClaimsPrincipal user, long resourceReserveId)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedException("Usuário não identificado.", true);
        }

        var resourceReserve = await dbCtx.ResourceReserves
            .Include(reserve => reserve.SpaceReserve)
            .FirstOrDefaultAsync(reserve => reserve.Id == resourceReserveId && reserve.FkAccountId == userId);

        if (resourceReserve == null)
        {
            throw new BadRequestException("A reserva de recurso informada não existe.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (resourceReserve.SpaceReserve.DateTo < today)
        {
            throw new BadRequestException("Não é possível cancelar uma reserva de recurso cuja reserva de espaço já terminou.");
        }

        if (resourceReserve.SpaceReserve.Status is EReserveStatus.Canceled or EReserveStatus.Rejected or EReserveStatus.Blocked)
        {
            throw new BadRequestException("A reserva de espaço não permite o cancelamento desta reserva de recurso no status atual.");
        }

        dbCtx.ResourceReserves.Remove(resourceReserve);
        await dbCtx.SaveChangesAsync();
    }
}