using System.ComponentModel.DataAnnotations;
using Models;

namespace DTOs.ResourceReserve;

/// <summary>Igual a <see cref="ResourceReserveEditRequestDTO"/>, mais o status.</summary>
public record AdminResourceReserveEditRequestDTO(
    EReserveStatus? Status,
    DateOnly? StartAt,
    DateOnly? EndAt,
    [MinLength(1, ErrorMessage = "É necessário informar pelo menos um horário para a reserva")]
    HashSet<long>? ScheduleIds,
    [Range(1, 100000, ErrorMessage = "A quantidade deve ser maior que zero")]
    int? Quantity);
