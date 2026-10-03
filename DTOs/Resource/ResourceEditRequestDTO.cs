using System.ComponentModel.DataAnnotations;

namespace DTOs.Resource;

public record ResourceEditRequestDTO(
    [Required]
    long Id,
    [StringLength(100, MinimumLength = 1)]
    string Name,
    string Description,
    bool CanReserve = true,
    bool Enabled = true);