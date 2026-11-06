using System.ComponentModel.DataAnnotations;
using Models;

namespace DTOs.Usage;

/// <summary>Uma linha do relatório de uso (um horário do turno).</summary>
/// <param name="NotebooksOut">Quantidade de notebooks na saída; obrigatória só no check-out.</param>
/// <param name="ChargersOut">Quantidade de fontes na saída; obrigatória só no check-out.</param>
public record UsageEntryInputDTO(
    [Required(ErrorMessage = "Informe o horário da linha do relatório.")]
    long ScheduleId,
    [Required(ErrorMessage = "Informe a turma.")]
    [StringLength(60, MinimumLength = 1, ErrorMessage = "A turma deve ter entre 1 e 60 caracteres.")]
    string ClassName,
    [Required(ErrorMessage = "Informe a modalidade.")]
    EUsageModality Modality,
    [Range(0, 999, ErrorMessage = "A quantidade de notebooks na entrada deve estar entre 0 e 999.")]
    int NotebooksIn,
    [Range(0, 999, ErrorMessage = "A quantidade de fontes na entrada deve estar entre 0 e 999.")]
    int ChargersIn,
    [Range(0, 999, ErrorMessage = "A quantidade de notebooks na saída deve estar entre 0 e 999.")]
    int? NotebooksOut = null,
    [Range(0, 999, ErrorMessage = "A quantidade de fontes na saída deve estar entre 0 e 999.")]
    int? ChargersOut = null);

/// <summary>Linhas do relatório enviadas no check-in, no preenchimento parcial ou no check-out.</summary>
/// <param name="RepeatForAll">Copia a primeira linha para todos os horários do turno.</param>
public record UsageEntriesRequestDTO(
    [Required(ErrorMessage = "Informe ao menos uma linha do relatório.")]
    [MinLength(1, ErrorMessage = "Informe ao menos uma linha do relatório.")]
    List<UsageEntryInputDTO> Entries,
    bool RepeatForAll = false);

/// <summary>Filtros das sessões do próprio usuário. Datas filtram o dia da sessão.</summary>
public record UsageRequestDTO(
    DateOnly? StartAt = null,
    DateOnly? EndAt = null,
    HashSet<EUsageStatus>? Statuses = null,
    [Range(1, 500, ErrorMessage = "O limite deve estar entre 1 e 500.")]
    int Limit = 30,
    [Range(0, int.MaxValue, ErrorMessage = "O deslocamento não pode ser negativo.")]
    int Offset = 0);

/// <summary>Filtros das sessões de todos os outros usuários (admin).</summary>
public record AdminUsageRequestDTO(
    [Required(ErrorMessage = "A data de início é obrigatória.")]
    DateOnly StartAt,
    [Required(ErrorMessage = "A data de término é obrigatória.")]
    DateOnly EndAt,
    HashSet<EUsageStatus>? Statuses = null,
    HashSet<long>? SpaceIds = null,
    HashSet<string>? AccountIds = null,
    [Range(1, 500, ErrorMessage = "O limite deve estar entre 1 e 500.")]
    int Limit = 30,
    [Range(0, int.MaxValue, ErrorMessage = "O deslocamento não pode ser negativo.")]
    int Offset = 0);
