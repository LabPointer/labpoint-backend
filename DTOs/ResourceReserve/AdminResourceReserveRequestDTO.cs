using System.ComponentModel.DataAnnotations;
using Models;

namespace DTOs.ResourceReserve;

/// <summary>Reservas de recurso de todos os usuários (admin). Datas filtram por sobreposição com o período.</summary>
/// <param name="SpaceIds">Sala da reserva vinculada.</param>
/// <param name="ResourceIds">Só reservas desses recursos (vazio ou nulo = todos).</param>
public record AdminResourceReserveRequestDTO(
    [Required(ErrorMessage = "A data de início é obrigatória")]
    DateOnly StartAt,
    [Required(ErrorMessage = "A data de término é obrigatória")]
    DateOnly EndAt,
    [StringLength(300, MinimumLength = 1)]
    string? SearchQuery,
    HashSet<long>? SpaceIds,
    HashSet<long>? ResourceIds,
    HashSet<string>? AccountIds,
    EReserveStatus? Status,
    [Range(1, 500, ErrorMessage = "O limite deve estar entre 1 e 500")]
    int Limit = 10,
    [Range(0, int.MaxValue, ErrorMessage = "O deslocamento deve ser maior ou igual a 0")]
    int Offset = 0
);
