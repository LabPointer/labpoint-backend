using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Calendar;

/// <summary>Marca que os feriados gerados (nacionais, estaduais e municipais) de um ano já estão no banco.</summary>
[Table("holiday_year_sync")]
public class HolidayYearSyncModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("year")]
    public int Year { get; set; }

    [Required]
    [Column("synced_at")]
    public DateTimeOffset SyncedAt { get; set; }

    /// <summary>false quando a BrasilAPI falhou e os feriados móveis foram calculados localmente (sem os estaduais).</summary>
    [Required]
    [Column("from_api")]
    public bool FromApi { get; set; }
}
