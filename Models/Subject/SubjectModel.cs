using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Models.Subject;

[Table("subject")]
[Index(nameof(Name), IsUnique = true)]
public class SubjectModel
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
    [Column("enabled")] 
    public bool Enabled { get; set; } = true;
    
    public virtual ICollection<AccountSubjectModel> AccountSubjects { get; set; } = new List<AccountSubjectModel>();
    
    public virtual ICollection<SpaceSubjectModel> SpaceSubjects { get; set; } = new List<SpaceSubjectModel>();
}