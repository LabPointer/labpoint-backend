using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Resource;
using Models.Subject;

namespace Models;

[Table("space_resource")]
[Index(nameof(FkSpaceId), nameof(FkResourceId), IsUnique = true)] 
public class SpaceResourceModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("fk_space_id")]
    [Required]
    public long FkSpaceId { get; set; }
    [ForeignKey(nameof(FkSpaceId))]
    public virtual SpaceModel Space { get; set; }
    
    [Column("fk_resource_id")]
    [Required]
    public long FkResourceId { get; set; }
    [ForeignKey(nameof(FkResourceId))]
    public virtual ResourceModel Resource { get; set; }
}