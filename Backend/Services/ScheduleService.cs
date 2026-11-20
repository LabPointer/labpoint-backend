using DTOs.Common;
using Backend.Handler;
using Data;
using DTOs.Schedule;
using DTOs.Subject;
using Microsoft.EntityFrameworkCore;
using Models.Schedule;
using Models.Subject;

namespace Backend.Services;

public interface IScheduleService
{
    public Task<List<ScheduleResponseDTO>> GetSchedules(ScheduleRequestDTO query);
    public Task AdminCreateSchedule(ScheduleCreateRequestDTO data);
    public Task AdminEditSchedule(ScheduleEditRequestDTO data);
    public Task AdminDeleteSchedule(AdminDeleteRequestDTO data);
}

public class ScheduleService(AppDbContext dbCtx) : IScheduleService
{
    public async Task<List<ScheduleResponseDTO>> GetSchedules(ScheduleRequestDTO query)
    {
        if (query.StartAt.HasValue && query.EndAt.HasValue && query.StartAt > query.EndAt)
            throw new BadRequestException("A hora de início precisa ser igual ou anterior à hora de fim.");

        var queryable = dbCtx.Schedules.AsNoTracking();
        
        if (query.Enabled.HasValue)
        {
            queryable = queryable.Where(s => s.Enabled == query.Enabled);
        }
        
        if (query.shift.HasValue)
        {
            queryable = queryable.Where(s => s.Shift == query.shift);
        }

        if (query.StartAt.HasValue)
        {
            queryable = queryable.Where(s => s.StartAt >= query.StartAt.Value);
        }

        if (query.EndAt.HasValue)
        {
            queryable = queryable.Where(s => s.EndAt <= query.EndAt.Value);
        }

        // Limit + 1: o item extra só indica ao front que existe próxima página.
        var schedules = await queryable
            .OrderBy(s => s.StartAt)
            .ThenBy(s => s.Id)
            .Skip(query.Offset)
            .Take(query.Limit + 1)
            .Select(s => new ScheduleResponseDTO(s.Id, s.StartAt, s.EndAt, s.Shift, s.Enabled))
            .ToListAsync();
        
        if (schedules.Count == 0)
            throw new ResourceNotFoundException("Nenhum horário encontrado");

        return schedules;
    }
    
    public async Task AdminCreateSchedule(ScheduleCreateRequestDTO data)
    {
        var subject = new ScheduleModel{ StartAt = data.StartAt, EndAt = data.EndAt, Shift = data.Shift, Enabled = data.Enabled};
        await dbCtx.Schedules.AddAsync(subject);
        await dbCtx.SaveChangesAsync();
    }
    
    public async Task AdminEditSchedule(ScheduleEditRequestDTO data)
    {
        var schedule = await dbCtx.Schedules.FindAsync(data.Id);
        if (schedule is null)
            throw new ResourceNotFoundException("Horario não encontrado");

        if (data.StartAt is null && data.EndAt is null && data.Shift is null && data.Enabled is null)
            throw new BadRequestException("Nenhum campo para atualizar foi fornecido");

        schedule.StartAt = data.StartAt ?? schedule.StartAt;
        schedule.EndAt = data.EndAt ?? schedule.EndAt;
        schedule.Shift = data.Shift ?? schedule.Shift;
        schedule.Enabled = data.Enabled ?? schedule.Enabled;
        
        await dbCtx.SaveChangesAsync();
    }

    public async Task AdminDeleteSchedule(AdminDeleteRequestDTO data)
    {
        var schedule = await dbCtx.Schedules.AsNoTracking().FirstOrDefaultAsync(s => s.Id == data.Id);
        if (schedule is null)
            throw new ResourceNotFoundException("Horário não encontrado.");

        PermanentDeletion.EnsureConfirmed($"{Label(schedule.StartAt)} - {Label(schedule.EndAt)}", data.ConfirmName);

        await using var transaction = await dbCtx.Database.BeginTransactionAsync();
        // O horário sai das reservas e dos relatórios de uso em cascata.
        await dbCtx.Schedules.Where(s => s.Id == schedule.Id).ExecuteDeleteAsync();
        // Reservas que ficaram sem nenhum horário deixam de fazer sentido e são removidas.
        await PermanentDeletion.DeleteSpaceReserves(dbCtx, dbCtx.SpaceReserves.Where(sr => !sr.SpaceReserveSchedules.Any()));
        await PermanentDeletion.DeleteResourceReserves(
            dbCtx, dbCtx.ResourceReserves.Where(rr => !rr.ResourceReserveSchedules.Any()));
        await transaction.CommitAsync();
    }

    private static string Label(TimeOnly time) => time.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);
}
