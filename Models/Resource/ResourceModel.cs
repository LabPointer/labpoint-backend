using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Reserve;

namespace Models.Resource;

/// <summary>
/// Recurso. Dois tipos:
/// - fixo (<see cref="Reservable"/> = false): característica de um espaço (ex.: "Televisão"), usado no filtro de salas;
/// - reservável (<see cref="Reservable"/> = true): item emprestável com <see cref="Quantity"/> unidades (ex.: "Notebooks"),
///   reservado sozinho ou junto com uma reserva de sala. Nunca é associado a um espaço.
/// </summary>
[Table("resource")]
[Index(nameof(Name), IsUnique = true)]
public class ResourceModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("description")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Column("enabled")]
    public bool Enabled { get; set; } = false;

    [Required]
    [Column("reservable")]
    public bool Reservable { get; set; } = false;

    /// <summary>Unidades disponíveis para reserva. Obrigatória (≥ 1) quando reservável; nula caso contrário.</summary>
    [Column("quantity")]
    public int? Quantity { get; set; }

    public virtual ICollection<SpaceResourceModel> SpaceResources { get; set; } = new List<SpaceResourceModel>();

    public virtual ICollection<ResourceReserveModel> ResourceReserves { get; set; } = new List<ResourceReserveModel>();
}
