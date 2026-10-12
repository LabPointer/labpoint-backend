using System.ComponentModel.DataAnnotations;

namespace DTOs.SpaceReserve;

public record ExistingScheduleRequestDTO(
    [Required(ErrorMessage = "A data de início é obrigatória")]
    DateOnly StartAt,
    [Required(ErrorMessage = "A data de término é obrigatória")]
    DateOnly EndAt);