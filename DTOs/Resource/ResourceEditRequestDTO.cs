using System.ComponentModel.DataAnnotations;

namespace DTOs.Resource;

public record ResourceEditRequestDTO(
    [Required(ErrorMessage = "O ID do recurso é obrigatório")]
    long Id,
    [StringLength(100, MinimumLength = 1, ErrorMessage = "O nome deve ter entre 1 e 100 caracteres")]
    string? Name,
    bool? CanReserve,
    bool? Enabled);