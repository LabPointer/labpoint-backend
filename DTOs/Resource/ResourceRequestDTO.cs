using System.ComponentModel.DataAnnotations;

namespace DTOs.Resource;

public record ResourceRequestDTO(
    [StringLength(100, MinimumLength = 1, ErrorMessage = "O nome deve ter entre 1 e 100 caracteres")]
    string? Name, 
    bool CanReserve = true, 
    bool Enabled = true,
    int Limit = 10,
    int Offset = 0);