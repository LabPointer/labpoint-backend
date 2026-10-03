using System.ComponentModel.DataAnnotations;
using Models;

namespace DTOs.Schedule;

public record ScheduleResponseDTO(
    [Required]
    long Id,
    TimeOnly StartAt,
    [Required]
    TimeOnly EndAt,
    [Required]
    EShift Shift);