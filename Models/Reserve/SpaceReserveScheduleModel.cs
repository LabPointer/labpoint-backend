using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Reserve;
using Models.Resource;
using Models.Schedule;

namespace Models;

[Table("space_reserve_schedule")]
[Index(nameof(FkSpaceReserveId), nameof(FkScheduleId), IsUnique = true)] 
public class SpaceReserveScheduleModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }
    
    [Required]
    [Column("fk_space_reserve_id")]
    public long FkSpaceReserveId { get; set; }
    [ForeignKey(nameof(FkSpaceReserveId))]
    public virtual SpaceReserveModel Space { get; set; }
    
    [Required]
    [Column("fk_schedule_id")]
    public long FkScheduleId { get; set; }
    [ForeignKey(nameof(FkScheduleId))]
    public virtual ScheduleModel Schedule { get; set; }
}