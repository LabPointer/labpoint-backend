using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models.Reserve;
using NpgsqlTypes;

namespace Models.Account;

[Table("account")]
[Index(nameof(Registration), IsUnique = true)]
public class AccountModel : IdentityUser
{
    [Required]
    [Column("registration")]
    [StringLength(16)]
    public string Registration { get; set; } = string.Empty;
    
    public NpgsqlTsVector SearchVector { get; set; } = null!;
    
    public virtual ICollection<AccountSubjectModel> AccountSubjects { get; set; } = new List<AccountSubjectModel>();
    
    public virtual ICollection<SpaceReserveModel> SpaceReserves { get; set; } = new List<SpaceReserveModel>();

    public virtual ICollection<ResourceReserveModel> ResourceReserves { get; set; } = new List<ResourceReserveModel>();
}