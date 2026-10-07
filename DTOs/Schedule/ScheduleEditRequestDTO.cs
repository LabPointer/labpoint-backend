using System.ComponentModel.DataAnnotations;
using Models;

namespace DTOs.Schedule;

public record ScheduleEditRequestDTO(
    [Required(ErrorMessage = "Id é obrigatorio")]
    long Id,
    TimeOnly? StartAt,
    TimeOnly ?EndAt,
    EShift? Shift,
    bool? Enabled);