using System.ComponentModel.DataAnnotations;

namespace DTOs.ResourceReserve;

/// <param name="ExcludeSpaceReserveId">Edição de uma reserva de sala: as unidades dela não contam como ocupadas.</param>
/// <param name="ExcludeResourceReserveId">Edição de uma reserva de recurso: as unidades dela não contam como ocupadas.</param>
public record ResourceAvailabilityRequestDTO(
    [Required(ErrorMessage = "A data de início é obrigatória")]
    DateOnly StartAt,
    [Required(ErrorMessage = "A data de término é obrigatória")]
    DateOnly EndAt,
    long? ExcludeSpaceReserveId,
    long? ExcludeResourceReserveId);

/// <summary>Menor quantidade livre do horário no período.</summary>
public record ResourceScheduleAvailabilityDTO(long ScheduleId, int Available);
