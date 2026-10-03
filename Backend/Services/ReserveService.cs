using System.Security.Claims;
using Backend.Handler;
using Data;
using DTOs.Schedule;
using DTOs.Space;
using DTOs.SpaceReserve;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Backend.Services;

public interface IReserveService
{
    public Task<List<SpaceReserveResponseDTO>> GetSpaceReserve(ClaimsPrincipal principal, SpaceReserveRequestDTO query);
}

public class ReserveService(AppDbContext dbCtx) : IReserveService
{
    public async Task<List<SpaceReserveResponseDTO>> GetSpaceReserve(ClaimsPrincipal principal, SpaceReserveRequestDTO query)
    {
        if (query.StartAt > query.EndAt)
        {
            throw new BadRequestException("Data de inicio precisa ser igual ou anterior a data de término.");
        }
    
        string userId = principal.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedException("Usuário não identificado.", true);
        }

        var user = await dbCtx.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new UnauthorizedException("Usuário não identificado.", true);
        }
        
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
                        sc.Schedule.Shift
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
}