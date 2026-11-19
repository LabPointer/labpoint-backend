namespace DTOs.Resource;

/// <param name="Quantity">Unidades do recurso reservável (nulo para recursos fixos de espaço).</param>
/// <param name="Available">
/// Unidades livres no período consultado (menor valor entre os dias e horários). Nulo quando a consulta não
/// informa período ou o recurso não é reservável.
/// </param>
public record ResourceResponseDTO(
    long Id,
    string Name,
    bool Enabled,
    string? Description,
    bool Reservable,
    int? Quantity,
    int? Available);
