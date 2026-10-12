using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models.Reserve;

namespace Models.Account;

[Table("account")]
[Index(nameof(Registration), IsUnique = true)]
public class AccountModel : IdentityUser
{
    [Required]
    [Column("registration")]
    [StringLength(16)]
    public string Registration { get; set; } = string.Empty;
    
    public virtual ICollection<AccountSubjectModel> AccountSubjects { get; set; } = new List<AccountSubjectModel>();
    
    public virtual ICollection<SpaceReserveModel> SpaceReserves { get; set; } = new List<SpaceReserveModel>();

    public virtual ICollection<ResourceReserveModel> ResourceReserves { get; set; } = new List<ResourceReserveModel>();
}