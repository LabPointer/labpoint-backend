using System.ComponentModel.DataAnnotations;
using Models;

namespace DTOs.Schedule;

public record ScheduleResponseDTO(
    long Id,
    TimeOnly StartAt,
    TimeOnly EndAt,
    EShift Shift,
    bool Enabled);