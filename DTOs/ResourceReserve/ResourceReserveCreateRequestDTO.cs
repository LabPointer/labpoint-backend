using System.ComponentModel.DataAnnotations;

namespace DTOs.ResourceReserve;

public record ResourceReserveCreateRequestDTO(
    [Range(1, long.MaxValue, ErrorMessage = "O ID do recurso deve ser maior que zero")]
    long ResourceId,
    [Required(ErrorMessage = "A finalidade da reserva é obrigatória")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "A finalidade deve ter entre 1 e 200 caracteres")]
    string Purpose);
