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
}

public class ScheduleService(AppDbContext dbCtx) : IScheduleService
{
    public async Task<List<ScheduleResponseDTO>> GetSchedules(ScheduleRequestDTO query)
    {
        var queryable = dbCtx.Schedules.AsQueryable();
        
        if (query.Enabled.HasValue)
        {
            queryable = queryable.Where(s => s.Enabled == query.Enabled);
        }
        
        if (query.shift.HasValue)
        {
            queryable = queryable.Where(s => s.Shift == query.shift);
        }

        var schedules = await queryable
            .OrderBy(s => s.StartAt)
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
}