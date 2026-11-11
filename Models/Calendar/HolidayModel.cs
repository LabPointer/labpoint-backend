using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Models.Calendar;

/// <summary>Feriado: bloqueia reservas no dia, a menos que a data tenha sido liberada por um admin.</summary>
[Table("holiday")]
[Index(nameof(Date))]
public class HolidayModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("date")]
    public DateOnly Date { get; set; }

    [Required]
    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Required]
    [Column("source")]
    public EHolidaySource Source { get; set; }

    [Required]
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
