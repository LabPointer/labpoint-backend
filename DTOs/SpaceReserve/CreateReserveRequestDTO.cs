using System.ComponentModel.DataAnnotations;

namespace DTOs.SpaceReserve;

public record SpaceReserveCreateRequestDTO(
    [Required(ErrorMessage = "A data de início é obrigatória")]
    DateOnly StartAt,
    [Required(ErrorMessage = "A data de término é obrigatória")]
    DateOnly EndAt,
    [Required(ErrorMessage = "Os IDs de horário são obrigatórios")]
    [MinLength(1, ErrorMessage = "Pelo menos um ID de horário é necessário")]
    HashSet<long> ScheduleIds,
    [Required(ErrorMessage = "O propósito da reserva é obrigatório")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "O propósito deve ter entre 1 e 200 caracteres")]
    string purpose);