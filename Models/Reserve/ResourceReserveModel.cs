using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Account;
using Models.Resource;
using Models.Usage;

namespace Models.Reserve;

/// <summary>
/// Reserva de N unidades de um recurso reservável.
/// Avulsa quando <see cref="FkSpaceReserveId"/> é nulo; vinculada a uma reserva de sala caso contrário.
/// A vinculada guarda uma cópia das datas e horários da sala, mantida em sincronia pelo serviço,
/// para que a checagem de disponibilidade seja a mesma nos dois casos.
/// </summary>
[Table("resource_reserve")]
public class ResourceReserveModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } =  DateTime.UtcNow;

    [Required]
    [Column("date_from")]
    public DateOnly DateFrom { get; set; }

    [Required]
    [Column("date_to")]
    public DateOnly DateTo { get; set; }

    [Required]
    [Column("status")]
    public EReserveStatus Status { get; set; }

    [Required]
    [Column("quantity")]
    public int Quantity { get; set; } = 1;

    [Required]
    [Column("purpose")]
    [StringLength(200)]
    public string Purpose{ get; set; }

    [Required]
    [Column("fk_resource_id")]
    public long FkResourceId { get; set; }
    [ForeignKey(nameof(FkResourceId))]
    public virtual ResourceModel Resource { get; set; }

    [Column("fk_space_reserve_id")]
    public long? FkSpaceReserveId { get; set; }
    [ForeignKey(nameof(FkSpaceReserveId))]
    public virtual SpaceReserveModel? SpaceReserve { get; set; }

    [Required]
    [Column("fk_account_id")]
    public string FkAccountId { get; set; }
    [ForeignKey(nameof(FkAccountId))]
    public virtual AccountModel Account { get; set; }

    public virtual ICollection<ResourceReserveScheduleModel> ResourceReserveSchedules { get; set; } = new List<ResourceReserveScheduleModel>();

    /// <summary>Sessões de uso próprias (só reservas avulsas; as vinculadas usam as da sala).</summary>
    public virtual ICollection<ReserveUsageModel> Usages { get; set; } = new List<ReserveUsageModel>();
}
