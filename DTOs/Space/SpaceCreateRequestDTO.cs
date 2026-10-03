using System.ComponentModel.DataAnnotations;

namespace DTOs.Space;

public record SpaceCreateRequestDTO(
    [Required]
    [MaxLength(100)]
    string Name,
    [Required]
    [MaxLength(200)]
    string Description,
    [Required]
    int Capacity,
    [Required]
    bool Locked,
    List<long> Subjects,
    List<long> Resources
);