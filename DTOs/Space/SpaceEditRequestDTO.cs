using System.ComponentModel.DataAnnotations;

namespace DTOs.Space;

public record SpaceEditRequestDTO(
    [Required]
    long Id,
    [MaxLength(100)]
    string? Name,
    [MaxLength(200)]
    string? Description,
    [Range(1, int.MaxValue)]
    int? Capacity,
    bool? Locked,
    HashSet<long> Subjects,
    HashSet<long> Resources);