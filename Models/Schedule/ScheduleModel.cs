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
    
    [Required]
    [Column("start_at")]
    public TimeOnly  StartAt { get; set; }
    
    [Required]
    [Column("end_at")]
    public TimeOnly EndAt { get; set; }
    
    [Required]
    [Column("shift")]
    public EShift Shift { get; set; }
}