using System.ComponentModel.DataAnnotations;

namespace DTOs.ResourceReserve;

/// <summary>Reserva avulsa de um recurso (sem sala).</summary>
public record ResourceReserveCreateRequestDTO(
    [Required(ErrorMessage = "A data de início é obrigatória")]
    DateOnly StartAt,
    [Required(ErrorMessage = "A data de término é obrigatória")]
    DateOnly EndAt,
    [Required(ErrorMessage = "Os IDs de horário são obrigatórios")]
    [MinLength(1, ErrorMessage = "Pelo menos um ID de horário é necessário")]
    HashSet<long> ScheduleIds,
    [Range(1, 100000, ErrorMessage = "A quantidade deve ser maior que zero")]
    int Quantity,
    [Required(ErrorMessage = "A finalidade da reserva é obrigatória")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "A finalidade deve ter entre 1 e 200 caracteres")]
    string Purpose);
