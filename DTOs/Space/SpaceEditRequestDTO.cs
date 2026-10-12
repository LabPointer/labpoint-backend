using System.ComponentModel.DataAnnotations;

namespace DTOs.Space;

public record SpaceEditRequestDTO(
    [Required(ErrorMessage = "O id do espaço é obrigatório")]
    long Id,
    [StringLength(100, MinimumLength = 1, ErrorMessage = "O nome deve ter entre 1 e 100 caracteres")]
    string? Name,
    [StringLength(200, MinimumLength = 1, ErrorMessage = "A descrição deve ter entre 1 e 200 caracteres")]
    string? Description,
    [Range(1, int.MaxValue)]
    int? Capacity,
    bool? Locked,
    HashSet<long>? Subjects,
    HashSet<long>? Resources);