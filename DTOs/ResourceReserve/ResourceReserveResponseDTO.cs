using DTOs.SpaceReserve;

namespace DTOs.ResourceReserve;

public record ResourceReserveResponseDTO(
    long Id,
    string Name,
    SpaceReserveResponseDTO SpaceReserve
);