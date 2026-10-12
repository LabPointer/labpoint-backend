using System.ComponentModel.DataAnnotations;

namespace DTOs.Space;

public record SpaceCreateRequestDTO(
    [Required(ErrorMessage = "O nome do espaço é obrigatório")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "O nome deve ter entre 1 e 100 caracteres")]
    string Name,
    [Required(ErrorMessage = "A descrição do espaço é obrigatória")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "A descrição deve ter entre 1 e 200 caracteres")]
    string Description,
    [Required(ErrorMessage = "A capacidade do espaço é obrigatória")]
    int Capacity,
    [Required(ErrorMessage = "O status de bloqueio do espaço é obrigatório")]
    bool Locked,
    List<long> Subjects,
    List<long> Resources
);