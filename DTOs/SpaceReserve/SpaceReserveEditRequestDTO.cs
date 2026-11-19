namespace DTOs.SpaceReserve;

/// <param name="Resources">
/// Conjunto final de recursos vinculados (adiciona, altera a quantidade e cancela os que saíram).
/// Nulo = não mexer nos recursos; lista vazia = cancelar todos.
/// </param>
public record SpaceReserveEditRequestDTO(
    DateOnly? StartAt,
    DateOnly? EndAt,
    HashSet<long>? ScheduleIds,
    List<ReserveResourceItemDTO>? Resources = null);
