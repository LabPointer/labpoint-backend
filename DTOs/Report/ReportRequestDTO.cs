using System.ComponentModel.DataAnnotations;

namespace DTOs.Report;

/// <summary>
/// Período do relatório: do mês (<paramref name="FromYear"/>/<paramref name="FromMonth"/>) até o mês
/// (<paramref name="ToYear"/>/<paramref name="ToMonth"/>), ambos inclusos. Para um mês só, basta omitir o fim.
/// </summary>
public record ReportRequestDTO(
    [Required(ErrorMessage = "O ano inicial do relatório é obrigatório")]
    [Range(2000, 9999, ErrorMessage = "O ano inicial do relatório é inválido")]
    int FromYear,
    [Required(ErrorMessage = "O mês inicial do relatório é obrigatório")]
    [Range(1, 12, ErrorMessage = "O mês inicial do relatório deve estar entre 1 e 12")]
    int FromMonth,
    [Range(2000, 9999, ErrorMessage = "O ano final do relatório é inválido")]
    int? ToYear = null,
    [Range(1, 12, ErrorMessage = "O mês final do relatório deve estar entre 1 e 12")]
    int? ToMonth = null,
    bool Refresh = false);
