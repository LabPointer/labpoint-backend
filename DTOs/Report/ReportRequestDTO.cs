using System.ComponentModel.DataAnnotations;

namespace DTOs.Report;

public record ReportRequestDTO(
    [Required(ErrorMessage = "O ano do relatório é obrigatório")]
    [Range(2000, 9999, ErrorMessage = "O ano do relatório é inválido")]
    int Year,
    [Required(ErrorMessage = "O mês do relatório é obrigatório")]
    [Range(1, 12, ErrorMessage = "O mês do relatório deve estar entre 1 e 12")]
    int Month,
    bool Refresh = false);
