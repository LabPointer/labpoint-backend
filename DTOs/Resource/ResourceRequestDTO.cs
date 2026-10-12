using System.ComponentModel.DataAnnotations;

namespace DTOs.Resource;

public record ResourceRequestDTO(
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "O nome deve ter entre 1 e 1000 caracteres")]
    string? Name, 
    bool? CanReserve, 
    bool? Enabled,
    [Range(1, int.MaxValue, ErrorMessage = "O limite deve ser maior que 0")]
    int Limit = 10,
    [Range(0, int.MaxValue, ErrorMessage = "O deslocamento deve ser maior ou igual a 0")]
    int Offset = 0);