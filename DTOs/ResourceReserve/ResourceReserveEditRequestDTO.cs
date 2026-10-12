using System.ComponentModel.DataAnnotations;

namespace DTOs.ResourceReserve;

public record ResourceReserveEditRequestDTO(
    [Range(1, long.MaxValue, ErrorMessage = "O ID da reserva de espaço deve ser maior que zero")]
    long SpaceReserveId,
    [StringLength(200, MinimumLength = 1, ErrorMessage = "A finalidade deve ter entre 1 e 200 caracteres")]
    string? Purpose);
