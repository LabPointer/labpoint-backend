using System.ComponentModel.DataAnnotations;
using Models;

namespace DTOs.Schedule;

public record ScheduleRequestDTO(
    EShift? shift,
    bool? Enabled,
    // Hora de início mínima: retorna horários que começam a partir dela.
    TimeOnly? StartAt,
    // Hora de fim máxima: retorna horários que terminam até ela.
    TimeOnly? EndAt,
    [Range(1, int.MaxValue, ErrorMessage = "O limite deve ser maior que 0")]
    int Limit = 10,
    [Range(0, int.MaxValue, ErrorMessage = "O deslocamento deve ser maior ou igual a 0")]
    int Offset = 0);