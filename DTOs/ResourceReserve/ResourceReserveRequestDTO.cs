using System.ComponentModel.DataAnnotations;

namespace DTOs.ResourceReserve;

public record ResourceReserveRequestDTO(
    [StringLength(300, MinimumLength = 1)]
    string? SearchQuery,
    [MinLength(1, ErrorMessage = "Deve haver pelo menos um ID de espaço fornecido.")]
    HashSet<long>? SpaceIds,
    int Limit = 10,
    int Offset = 0
);