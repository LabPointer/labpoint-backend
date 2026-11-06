using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Account;
using Models.Reserve;

namespace Models.Usage;

/// <summary>
/// Sessão de uso de uma reserva: um turno (manhã, tarde ou noite) de um dia. Tem um check-in (no primeiro horário do
/// turno) e um check-out (no último). Reserva de sala cobre também os recursos vinculados a ela; reserva de recurso
/// avulsa tem sessões próprias. Exatamente um entre <see cref="FkSpaceReserveId"/> e <see cref="FkResourceReserveId"/>.
/// </summary>
[Table("reserve_usage")]
[Index(nameof(FkSpaceReserveId), nameof(Date), nameof(Shift), IsUnique = true)]
[Index(nameof(FkResourceReserveId), nameof(Date), nameof(Shift), IsUnique = true)]
[Index(nameof(Date), nameof(Status))]
[Index(nameof(CheckInTokenHash))]
[Index(nameof(CheckOutTokenHash))]
public class ReserveUsageModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("fk_space_reserve_id")]
    public long? FkSpaceReserveId { get; set; }
    [ForeignKey(nameof(FkSpaceReserveId))]
    public virtual SpaceReserveModel? SpaceReserve { get; set; }

    [Column("fk_resource_reserve_id")]
    public long? FkResourceReserveId { get; set; }
    [ForeignKey(nameof(FkResourceReserveId))]
    public virtual ResourceReserveModel? ResourceReserve { get; set; }

    [Required]
    [Column("fk_account_id")]
    public string FkAccountId { get; set; }
    [ForeignKey(nameof(FkAccountId))]
    public virtual AccountModel Account { get; set; }

    [Required]
    [Column("date")]
    public DateOnly Date { get; set; }

    [Required]
    [Column("shift")]
    public EShift Shift { get; set; }

    [Required]
    [Column("status")]
    public EUsageStatus Status { get; set; }

    [Column("check_in_opens_at")]
    public DateTimeOffset CheckInOpensAt { get; set; }

    [Column("check_in_closes_at")]
    public DateTimeOffset CheckInClosesAt { get; set; }

    [Column("check_out_opens_at")]
    public DateTimeOffset CheckOutOpensAt { get; set; }

    [Column("check_out_closes_at")]
    public DateTimeOffset CheckOutClosesAt { get; set; }

    [Column("checked_in_at")]
    public DateTimeOffset? CheckedInAt { get; set; }

    [Column("checked_out_at")]
    public DateTimeOffset? CheckedOutAt { get; set; }

    /// <summary>Admin que fechou o check-out no lugar do usuário (nulo quando o próprio usuário fechou).</summary>
    [Column("fk_checked_out_by_id")]
    public string? FkCheckedOutById { get; set; }
    [ForeignKey(nameof(FkCheckedOutById))]
    public virtual AccountModel? CheckedOutBy { get; set; }

    [Column("check_in_email_sent_at")]
    public DateTimeOffset? CheckInEmailSentAt { get; set; }

    [Column("check_out_email_sent_at")]
    public DateTimeOffset? CheckOutEmailSentAt { get; set; }

    /// <summary>SHA-256 (hex) do token do link de check-in. Limpo depois de usado ou expirado.</summary>
    [Column("check_in_token_hash")]
    [StringLength(64)]
    public string? CheckInTokenHash { get; set; }

    /// <summary>SHA-256 (hex) do token do link de check-out. Limpo depois de usado ou expirado.</summary>
    [Column("check_out_token_hash")]
    [StringLength(64)]
    public string? CheckOutTokenHash { get; set; }

    public virtual ICollection<ReserveUsageEntryModel> Entries { get; set; } = new List<ReserveUsageEntryModel>();
}
