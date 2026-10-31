using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Models.Report;

/// <summary>
/// Relatório já calculado para um período de meses (de um mês único até uma sequência deles).
/// Só é gravado quando alguém solicita aquele período pela primeira vez (ou força a atualização),
/// assim as próximas consultas não precisam varrer reservas, espaços e contas de novo.
/// </summary>
[Table("report")]
[Index(nameof(FromYear), nameof(FromMonth), nameof(ToYear), nameof(ToMonth), IsUnique = true)]
public class ReportModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>Ano do primeiro mês do período.</summary>
    [Required]
    [Column("from_year")]
    public int FromYear { get; set; }

    /// <summary>Primeiro mês do período, de 1 a 12.</summary>
    [Required]
    [Column("from_month")]
    public int FromMonth { get; set; }

    /// <summary>Ano do último mês do período (igual ao inicial quando o período é de um mês só).</summary>
    [Required]
    [Column("to_year")]
    public int ToYear { get; set; }

    /// <summary>Último mês do período, de 1 a 12 (igual ao inicial quando o período é de um mês só).</summary>
    [Required]
    [Column("to_month")]
    public int ToMonth { get; set; }

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
