using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Schedule;

[Table("shift")]
public class ScheduleModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("start_at")]
    [Required]
    public TimeOnly  StartAt { get; set; }
    
    [Column("end_at")]
    [Required]
    public TimeOnly EndAt { get; set; }
    
    [Column("shift")]
    [Required]
    public EShift Shift { get; set; }
}