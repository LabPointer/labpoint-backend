using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Account;
using Models.Resource;

namespace Models.Reserve;

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
    [Column("purpose")]
    [StringLength(200)]
    public string Purpose{ get; set; }

    [Required]
    [Column("fk_resource_id")]
    public long FkResourceId { get; set; }
    [ForeignKey(nameof(FkResourceId))]
    public virtual ResourceModel Resource { get; set; }
    
    [Required]
    [Column("fk_space_reserve_id")]
    public long FkSpaceReserveId { get; set; }
    [ForeignKey(nameof(FkSpaceReserveId))]
    public virtual SpaceReserveModel SpaceReserve { get; set; }
    
    [Required]
    [Column("fk_account_id")]
    public string FkAccountId { get; set; }
    [ForeignKey(nameof(FkAccountId))]
    public virtual AccountModel Account { get; set; }
}