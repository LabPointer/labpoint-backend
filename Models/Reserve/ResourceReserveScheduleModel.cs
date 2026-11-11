using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Schedule;

namespace Models.Reserve;

[Table("resource_reserve_schedule")]
[Index(nameof(FkResourceReserveId), nameof(FkScheduleId), IsUnique = true)]
public class ResourceReserveScheduleModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("fk_resource_reserve_id")]
    public long FkResourceReserveId { get; set; }
    [ForeignKey(nameof(FkResourceReserveId))]
    public virtual ResourceReserveModel ResourceReserve { get; set; } = null!;

    [Required]
    [Column("fk_schedule_id")]
    public long FkScheduleId { get; set; }
    [ForeignKey(nameof(FkScheduleId))]
    public virtual ScheduleModel Schedule { get; set; } = null!;
}
