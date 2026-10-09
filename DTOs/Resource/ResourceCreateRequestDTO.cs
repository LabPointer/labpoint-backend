namespace DTOs.Resource;
using System.ComponentModel.DataAnnotations;

public record ResourceCreateRequestDTO(
    [Required(ErrorMessage = "O nome do recurso é obrigatório")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "O nome deve ter entre 1 e 100 caracteres")]
    string Name,
    [Required(ErrorMessage = "A descrição do recurso é obrigatória")]
    bool CanReserve = false,
    bool Enabled = true);