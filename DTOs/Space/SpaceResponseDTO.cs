using System.ComponentModel.DataAnnotations;

namespace DTOs.Space;

public record SpaceHashDataDTO(
    long Id,
    string Name);

public record SpaceResponseDTO(
    long Id,
    string Name,
    int Capacity,
    string Description,
    bool Locked,
    HashSet<SpaceHashDataDTO> Subjects,
    HashSet<SpaceHashDataDTO> Resources);