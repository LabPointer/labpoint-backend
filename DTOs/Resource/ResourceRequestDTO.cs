using System.ComponentModel.DataAnnotations;

namespace DTOs.Resource;

/// <summary>
/// Filtros da listagem de recursos. Com <paramref name="StartAt"/>/<paramref name="EndAt"/> informados, cada recurso
/// reservável volta com <c>Available</c> (menor número de unidades livres no período e nos horários pedidos) e os que
/// têm menos de <paramref name="MinQuantity"/> unidades livres ficam de fora.
/// </summary>
/// <param name="Name">Busca antiga só por nome (mantida para os comboboxes); prefira <paramref name="SearchQuery"/>.</param>
/// <param name="SearchQuery">Busca em nome e descrição (full-text em português + trecho parcial).</param>
/// <param name="ExcludeSpaceReserveId">Na edição de uma reserva de sala: as unidades dela não contam como ocupadas.</param>
/// <param name="ExcludeResourceReserveId">Na edição de uma reserva de recurso: as unidades dela não contam como ocupadas.</param>
public record ResourceRequestDTO(
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "O nome deve ter entre 1 e 1000 caracteres")]
    string? Name, 
    bool? Enabled,
    [Range(1, int.MaxValue, ErrorMessage = "O limite deve ser maior que 0")]
    int Limit = 10,
    [Range(0, int.MaxValue, ErrorMessage = "O deslocamento deve ser maior ou igual a 0")]
    int Offset = 0,
    [StringLength(300, MinimumLength = 1, ErrorMessage = "A busca deve ter entre 1 e 300 caracteres")]
    string? SearchQuery = null,
    bool? Reservable = null,
    DateOnly? StartAt = null,
    DateOnly? EndAt = null,
    HashSet<long>? ScheduleIds = null,
    [Range(1, int.MaxValue, ErrorMessage = "A quantidade mínima deve ser maior que 0")]
    int? MinQuantity = null,
    long? ExcludeSpaceReserveId = null,
    long? ExcludeResourceReserveId = null);
