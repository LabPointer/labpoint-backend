using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Account;
using Models.Subject;

namespace Models;

[Table("account_subject")]
[Index(nameof(FkAccountId), nameof(FkSubjectId), IsUnique = true)] 
public class AccountSubjectModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }
    
    [Required]
    [Column("fk_account_id")]
    public string FkAccountId { get; set; }
    [ForeignKey(nameof(FkAccountId))]
    public virtual AccountModel Account { get; set; }
    
    [Required]
    [Column("fk_subject_id")]
    public long FkSubjectId { get; set; }
    [ForeignKey(nameof(FkSubjectId))]
    public virtual SubjectModel Subject { get; set; }
}