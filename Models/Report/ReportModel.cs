using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Models.Report;

/// <summary>
/// Relatório mensal já calculado. Só é gravado quando alguém solicita aquele mês pela primeira vez
/// (ou força a atualização), assim as próximas consultas não precisam varrer reservas, espaços e contas de novo.
/// </summary>
[Table("report")]
[Index(nameof(Year), nameof(Month), IsUnique = true)]
public class ReportModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("year")]
    public int Year { get; set; }

    /// <summary>Mês de 1 a 12.</summary>
    [Required]
    [Column("month")]
    public int Month { get; set; }

    /// <summary>
    /// Versão do formato de <see cref="Data"/>. Quando o cálculo ou o DTO mudam, o serviço sobe a versão
    /// e os relatórios gravados com versão antiga são recalculados na próxima consulta.
    /// </summary>
    [Required]
    [Column("version")]
    public int Version { get; set; }

    [Required]
    [Column("generated_at")]
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Relatório serializado em JSON (jsonb).</summary>
    [Required]
    [Column("data", TypeName = "jsonb")]
    public string Data { get; set; } = "{}";
}
