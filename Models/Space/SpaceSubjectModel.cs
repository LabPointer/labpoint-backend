using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Subject;

namespace Models;

[Table("space_subject")]
[Index(nameof(FkSpaceId), nameof(FkSubjectId), IsUnique = true)] 
public class SpaceSubjectModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }
    
    [Required]
    [Column("fk_space_id")]
    public long FkSpaceId { get; set; }
    [ForeignKey(nameof(FkSpaceId))]
    public virtual SpaceModel Space { get; set; }
    
    [Required]
    [Column("fk_subject_id")]
    public long FkSubjectId { get; set; }
    [ForeignKey(nameof(FkSubjectId))]
    public virtual SubjectModel Subject { get; set; }
}