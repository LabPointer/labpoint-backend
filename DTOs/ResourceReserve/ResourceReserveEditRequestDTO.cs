using System.ComponentModel.DataAnnotations;

namespace DTOs.ResourceReserve;

/// <summary>
/// Campos nulos não são alterados. StartAt e EndAt precisam ser enviados juntos.
/// Em reserva vinculada a uma sala, mudar período ou horários altera também a reserva da sala
/// (e os demais recursos dela), checando a disponibilidade de todos.
/// </summary>
public record ResourceReserveEditRequestDTO(
    DateOnly? StartAt,
    DateOnly? EndAt,
    [MinLength(1, ErrorMessage = "É necessário informar pelo menos um horário para a reserva")]
    HashSet<long>? ScheduleIds,
    [Range(1, 100000, ErrorMessage = "A quantidade deve ser maior que zero")]
    int? Quantity);
