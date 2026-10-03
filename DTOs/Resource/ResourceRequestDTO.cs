using System.ComponentModel.DataAnnotations;

namespace DTOs.Resource;

public record ResourceRequestDTO(
    [StringLength(100, MinimumLength = 1)]
    string Name, 
    bool CanReserve = true, 
    bool Enabled = true,
    int Limit = 10,
    int Offset = 0);