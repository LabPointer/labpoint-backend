using System.ComponentModel.DataAnnotations;

namespace DTOs.Space;

public record SpaceHashDataDTO(
    long Id,
    string Name);

public record SpaceResponseDTO(
    [Required]
    long Id,
    [Required]
    string Name,
    [Required]
    int Capacity,
    [Required]
    string Description,
    [Required]
    bool Locked,
    HashSet<SpaceHashDataDTO> Subjects,
    HashSet<SpaceHashDataDTO> Resources);