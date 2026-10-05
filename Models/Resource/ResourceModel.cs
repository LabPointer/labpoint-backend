using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Models.Resource;

[Table("resource")]
[Index(nameof(Name), IsUnique = true)]
public class ResourceModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }
    
    [Required]
    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; }
    
    [Column("description")]
    [StringLength(200)]
    public string Description { get; set; }

    [Required]
    [Column("can_reserve")]
    public bool CanReserve { get; set; } = false;
    
    [Required]
    [Column("enabled")]
    public bool Enabled { get; set; } = false;
    
    public NpgsqlTsVector SearchVector { get; set; } = null!;
    
    public virtual ICollection<SpaceResourceModel> SpaceResources { get; set; } = new List<SpaceResourceModel>();
}