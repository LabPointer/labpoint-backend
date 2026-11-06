using System.ComponentModel.DataAnnotations;

namespace DTOs.Resource;

/// <summary>Campos nulos não são alterados. Descrição vazia ("") remove a descrição.</summary>
public record ResourceEditRequestDTO(
    [Required(ErrorMessage = "O ID do recurso é obrigatório")]
    long Id,
    [StringLength(100, MinimumLength = 1, ErrorMessage = "O nome deve ter entre 1 e 100 caracteres")]
    string? Name,
    bool? Enabled,
    [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres")]
    string? Description = null,
    bool? Reservable = null,
    [Range(1, 100000, ErrorMessage = "A quantidade deve estar entre 1 e 100000")]
    int? Quantity = null);
