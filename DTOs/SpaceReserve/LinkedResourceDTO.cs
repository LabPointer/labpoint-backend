using Models;

namespace DTOs.SpaceReserve;

/// <summary>Reserva de recurso vinculada a uma reserva de sala.</summary>
public record LinkedResourceDTO(
    long ReserveId,
    long ResourceId,
    string Name,
    string? Description,
    int Quantity,
    EReserveStatus Status);
