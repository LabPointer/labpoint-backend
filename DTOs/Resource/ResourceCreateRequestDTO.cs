using System.ComponentModel.DataAnnotations;

namespace DTOs.Resource;

/// <param name="Quantity">Obrigatória (≥ 1) quando <paramref name="Reservable"/>; ignorada caso contrário.</param>
public record ResourceCreateRequestDTO(
    [Required(ErrorMessage = "O nome do recurso é obrigatório")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "O nome deve ter entre 1 e 100 caracteres")]
    string Name,
    [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres")]
    string? Description = null,
    bool Enabled = true,
    bool Reservable = false,
    [Range(1, 100000, ErrorMessage = "A quantidade deve estar entre 1 e 100000")]
    int? Quantity = null);
