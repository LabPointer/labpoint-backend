using System.ComponentModel.DataAnnotations;
using Models;

namespace DTOs.Schedule;

public record ScheduleCreateRequestDTO(
    [Required(ErrorMessage = "Hora de inicio é obrigatorio")]
    TimeOnly StartAt,
    [Required(ErrorMessage = "Hora de fim é obrigatorio")]
    TimeOnly EndAt,
    [Required(ErrorMessage = "Turno é obrigatorio")]
    EShift Shift,
    [Required(ErrorMessage = "Habilitado é obrigatorio")]
    bool Enabled);