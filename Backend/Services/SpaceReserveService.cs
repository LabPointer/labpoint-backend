using System.Linq.Expressions;
using System.Security.Claims;
using Backend.Handler;
using Data;
using DTOs.Account;
using DTOs.Schedule;
using DTOs.Space;
using DTOs.SpaceReserve;
using DTOs.Subject;
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
    /// <summary>Só reservas pendentes ou confirmadas ocupam horários.</summary>
    private static readonly Expression<Func<SpaceReserveModel, bool>> IsActive =
        sr => sr.Status == EReserveStatus.Booking || sr.Status == EReserveStatus.Confirmed;

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
                    .ToList()
            ))
            .AsSplitQuery()
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

        var existingSchedules = await dbCtx.SpaceReserves
            .Where(sr => sr.FkSpaceId == spaceId && sr.DateFrom <= query.EndAt && sr.DateTo >= query.StartAt)
            .Where(IsActive)
            .SelectMany(sr => sr.SpaceReserveSchedules.Select(s => new ScheduleResponseDTO(
                s.Schedule.Id,
                s.Schedule.StartAt,
                s.Schedule.EndAt,
                s.Schedule.Shift,
                s.Schedule.Enabled
            )))
            .Distinct()
            .ToListAsync();

        if (existingSchedules.Count == 0)
        {
            throw new ResourceNotFoundException("Nenhum horário existente encontrado para o período informado.");
        }

        return existingSchedules;
    }

    public async Task CreateSpaceReserve(ClaimsPrincipal user, long spaceId, SpaceReserveCreateRequestDTO data)
    {
        string userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        if (data.StartAt > data.EndAt)
        {
            throw new BadRequestException("Data de início precisa ser igual ou anterior à data de término.");
        }

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

        await EnsureSchedulesAvailable(data.ScheduleIds);

        bool hasConflict = await dbCtx.SpaceReserves
            .Where(sr => sr.FkSpaceId == spaceId && sr.DateFrom <= data.EndAt && sr.DateTo >= data.StartAt)
            .Where(IsActive)
            .AnyAsync(sr => sr.SpaceReserveSchedules.Any(s => data.ScheduleIds.Contains(s.FkScheduleId)));

        if (hasConflict)
        {
            throw new BadRequestException("A reserva conflita com uma reserva existente no mesmo período e horário.");
        }

        var hasElevatedRole = user.IsInRole("Owner") || user.IsInRole("Admin");
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

        if ((spaceReserve.Status == EReserveStatus.Canceled || spaceReserve.Status == EReserveStatus.Rejected) && !(user.IsInRole("Owner") || user.IsInRole("Admin")))
        {
            throw new BadRequestException("Não é possível editar uma reserva cancelada ou rejeitada.");
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

        bool schedulesChanged = data.ScheduleIds != null && data.ScheduleIds.Count > 0;
        var currentScheduleIds = spaceReserve.SpaceReserveSchedules.Select(s => s.FkScheduleId).ToHashSet();

        if (schedulesChanged)
        {
            await EnsureSchedulesAvailable(data.ScheduleIds!.Where(id => !currentScheduleIds.Contains(id)).ToHashSet());
        }

        var targetStart = data.StartAt ?? spaceReserve.DateFrom;
        var targetEnd = data.EndAt ?? spaceReserve.DateTo;
        var targetScheduleIds = schedulesChanged ? data.ScheduleIds! : currentScheduleIds;

        if (targetScheduleIds.Any())
        {
            await EnsureNoConflict(reserveId, spaceReserve.FkSpaceId, targetStart, targetEnd, targetScheduleIds);
        }

        if (dataChanged)
        {
            spaceReserve.DateFrom = data.StartAt!.Value;
            spaceReserve.DateTo = data.EndAt!.Value;
        }

        if (schedulesChanged)
        {
            ReplaceSchedules(spaceReserve, data.ScheduleIds!);
        }

        await dbCtx.SaveChangesAsync();
    }

    public async Task CancelSpaceReserve(ClaimsPrincipal user, long reserveId)
    {
        string userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        // Um único UPDATE (antes: SELECT + UPDATE).
        var affected = await dbCtx.SpaceReserves
            .Where(sr => sr.Id == reserveId && sr.FkAccountId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(sr => sr.Status, EReserveStatus.Canceled));

        if (affected == 0)
        {
            throw new ResourceNotFoundException("Reserva não encontrada.");
        }
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
                row.Schedules))
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

        if (data.Status == null && !datesChanged && !schedulesChanged)
        {
            throw new BadRequestException("Informe ao menos um campo para alterar.");
        }

        if (datesChanged)
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

        var currentScheduleIds = spaceReserve.SpaceReserveSchedules.Select(s => s.FkScheduleId).ToHashSet();

        if (schedulesChanged)
        {
            await EnsureSchedulesAvailable(data.ScheduleIds!.Where(id => !currentScheduleIds.Contains(id)).ToHashSet());
        }

        var targetStart = data.StartAt ?? spaceReserve.DateFrom;
        var targetEnd = data.EndAt ?? spaceReserve.DateTo;
        var targetStatus = data.Status ?? spaceReserve.Status;
        var targetScheduleIds = schedulesChanged ? data.ScheduleIds! : currentScheduleIds;

        // Só reservas ativas ocupam horários: só há conflito se a reserva ficar (ou continuar) ativa.
        bool willBeActive = targetStatus == EReserveStatus.Booking || targetStatus == EReserveStatus.Confirmed;
        if (willBeActive && targetScheduleIds.Any())
        {
            await EnsureNoConflict(reserveId, spaceReserve.FkSpaceId, targetStart, targetEnd, targetScheduleIds);
        }

        if (datesChanged)
        {
            spaceReserve.DateFrom = data.StartAt!.Value;
            spaceReserve.DateTo = data.EndAt!.Value;
        }

        if (schedulesChanged)
        {
            ReplaceSchedules(spaceReserve, data.ScheduleIds!);
        }

        spaceReserve.Status = targetStatus;
        await dbCtx.SaveChangesAsync();
    }

    /// <summary>Os horários novos precisam existir e estar ativos (uma consulta só).</summary>
    private async Task EnsureSchedulesAvailable(HashSet<long> scheduleIds)
    {
        if (scheduleIds.Count == 0)
        {
            return;
        }

        var validCount = await dbCtx.Schedules.CountAsync(s => scheduleIds.Contains(s.Id) && s.Enabled);
        if (validCount != scheduleIds.Count)
        {
            throw new BadRequestException("Um ou mais horários informados não existem ou estão desativados.");
        }
    }

    private async Task EnsureNoConflict(long reserveId, long spaceId, DateOnly start, DateOnly end, HashSet<long> scheduleIds)
    {
        bool hasConflict = await dbCtx.SpaceReserves
            .Where(sr => sr.Id != reserveId && sr.FkSpaceId == spaceId && sr.DateFrom <= end && sr.DateTo >= start)
            .Where(IsActive)
            .AnyAsync(sr => sr.SpaceReserveSchedules.Any(s => scheduleIds.Contains(s.FkScheduleId)));

        if (hasConflict)
        {
            throw new BadRequestException("A reserva conflita com uma reserva existente no mesmo período e horário.");
        }
    }

    private static void ReplaceSchedules(SpaceReserveModel spaceReserve, HashSet<long> scheduleIds)
    {
        spaceReserve.SpaceReserveSchedules.Clear();
        foreach (var scheduleId in scheduleIds)
        {
            spaceReserve.SpaceReserveSchedules.Add(new SpaceReserveScheduleModel
            {
                FkSpaceReserveId = spaceReserve.Id,
                FkScheduleId = scheduleId
            });
        }
    }
}
