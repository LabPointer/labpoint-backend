namespace DTOs.Resource;
using System.ComponentModel.DataAnnotations;

public record ResourceCreateRequestDTO(
    [Required(ErrorMessage = "O nome do recurso é obrigatório")]
    [StringLength(100, MinimumLength = 1)]
    string Name,
    [Required(ErrorMessage = "A descrição do recurso é obrigatória")]
    [StringLength(200, MinimumLength = 1)]
    string Description,
    bool CanReserve = false,
    bool Enabled = true);