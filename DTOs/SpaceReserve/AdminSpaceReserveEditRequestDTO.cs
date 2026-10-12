using System.ComponentModel.DataAnnotations;
using Models;

namespace DTOs.SpaceReserve;

/// <summary>
/// Todos os campos são opcionais (null = não alterar), mas pelo menos um deve ser informado.
/// StartAt e EndAt precisam ser enviados juntos.
/// </summary>
public record AdminSpaceReserveEditRequestDTO(
    EReserveStatus? Status,
    DateOnly? StartAt,
    DateOnly? EndAt,
    [MinLength(1, ErrorMessage = "É necessário informar pelo menos um horário para a reserva")]
    HashSet<long>? ScheduleIds);