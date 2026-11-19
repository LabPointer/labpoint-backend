using System.ComponentModel.DataAnnotations;

namespace DTOs.SpaceReserve;

/// <summary>Recurso reservável pedido junto com uma reserva de sala.</summary>
public record ReserveResourceItemDTO(
    [Range(1, long.MaxValue, ErrorMessage = "O ID do recurso deve ser maior que zero")]
    long ResourceId,
    [Range(1, 100000, ErrorMessage = "A quantidade deve ser maior que zero")]
    int Quantity);
