using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Account;

namespace Models.Calendar;

/// <summary>
/// Fim de semana ou feriado liberado por um admin para reservas.
/// Ativa enquanto <see cref="RevokedAt"/> é nulo. Revogada com <see cref="KeepExistingReserves"/>, o dia volta a
/// ficar bloqueado para reservas novas, mas continua valendo para as criadas antes da revogação.
/// </summary>
[Table("released_date")]
[Index(nameof(Date), IsUnique = true)]
public class ReleasedDateModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("date")]
    public DateOnly Date { get; set; }

    [Required]
    [Column("fk_account_id")]
    public string FkAccountId { get; set; } = null!;
    [ForeignKey(nameof(FkAccountId))]
    public virtual AccountModel Account { get; set; } = null!;

    [Required]
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("revoked_at")]
    public DateTimeOffset? RevokedAt { get; set; }

    [Required]
    [Column("keep_existing_reserves")]
    public bool KeepExistingReserves { get; set; }
}
