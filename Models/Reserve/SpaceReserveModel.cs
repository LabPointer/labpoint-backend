using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Reserve;

[Table("space_reserve")]
public class SpaceReserveModel
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
    public DateOnly DateFrom{ get; set; }
    
    [Required]
    [Column("date_to")]
    public DateOnly DateTo{ get; set; }
    
    [Required]
    [Column("purpose")]
    [StringLength(200)]
    public string Purpose{ get; set; }
    
    [Required]
    [Column("status")]
    public EReserveStatus Status{ get; set; }
    
    [Required]
    [Column("fk_space_id")]
    public long FkSpaceId { get; set; }
    [ForeignKey(nameof(FkSpaceId))]
    public virtual SpaceModel Space { get; set; }
    
    [Required]
    [Column("fk_account_id")]
    public string FkAccountId { get; set; }
    [ForeignKey(nameof(FkAccountId))]
    public virtual AccountModel Account { get; set; }
    
    public virtual ICollection<SpaceReserveScheduleModel> SpaceReserveSchedules { get; set; } =  new List<SpaceReserveScheduleModel>();
}