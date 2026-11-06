using System.ComponentModel.DataAnnotations;
using Models;

namespace DTOs.ResourceReserve;

/// <summary>Minhas reservas de recurso. Datas filtram por sobreposição com o período.</summary>
/// <param name="SearchQuery">Busca no nome e na descrição do recurso.</param>
/// <param name="Standalone">true = só avulsas; false = só vinculadas a uma sala; nulo = todas.</param>
public record ResourceReserveRequestDTO(
    [StringLength(300, MinimumLength = 1)]
    string? SearchQuery,
    DateOnly? StartAt,
    DateOnly? EndAt,
    HashSet<long>? ScheduleIds,
    EReserveStatus? Status,
    bool? Standalone,
    [Range(1, 500, ErrorMessage = "O limite deve estar entre 1 e 500")]
    int Limit = 10,
    [Range(0, int.MaxValue, ErrorMessage = "O deslocamento deve ser maior ou igual a 0")]
    int Offset = 0
);
