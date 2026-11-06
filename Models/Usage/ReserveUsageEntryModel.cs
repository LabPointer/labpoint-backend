using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Schedule;

namespace Models.Usage;

/// <summary>
/// Linha do relatório de uso ("Controle de notebooks — uso diário") para um horário da sessão: turma, modalidade e
/// quantidades de notebooks e fontes na entrada e na saída. As de saída ficam nulas até o check-out.
/// </summary>
[Table("reserve_usage_entry")]
[Index(nameof(FkUsageId), nameof(FkScheduleId), IsUnique = true)]
public class ReserveUsageEntryModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("fk_usage_id")]
    public long FkUsageId { get; set; }
    [ForeignKey(nameof(FkUsageId))]
    public virtual ReserveUsageModel Usage { get; set; }

    [Required]
    [Column("fk_schedule_id")]
    public long FkScheduleId { get; set; }
    [ForeignKey(nameof(FkScheduleId))]
    public virtual ScheduleModel Schedule { get; set; }

    [Required]
    [Column("class_name")]
    [StringLength(60)]
    public string ClassName { get; set; }

    [Required]
    [Column("modality")]
    public EUsageModality Modality { get; set; }

    [Column("notebooks_in")]
    public int NotebooksIn { get; set; }

    [Column("chargers_in")]
    public int ChargersIn { get; set; }

    [Column("notebooks_out")]
    public int? NotebooksOut { get; set; }

    [Column("chargers_out")]
    public int? ChargersOut { get; set; }
}
