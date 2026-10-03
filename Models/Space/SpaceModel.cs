using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Reserve;

namespace Models;

[Table("space")]
[Index(nameof(Name), IsUnique = true)] 
public class SpaceModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }
    
    [Required]
    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; }

    [Required]
    [Column("description")]
    [StringLength(200)]
    public String Description;

    [Required]
    [Column("capacity")]
    public int Capacity;

    [Required]
    [Column("locked")]
    public bool Locked;
    
    public virtual ICollection<SpaceReserveModel> SpaceReserves { get; set; } =  new List<SpaceReserveModel>();
    
    public virtual ICollection<SpaceSubjectModel> SpaceSubjects { get; set; } = new List<SpaceSubjectModel>();
    
    public virtual ICollection<SpaceResourceModel> SpaceResources { get; set; } = new List<SpaceResourceModel>();
}