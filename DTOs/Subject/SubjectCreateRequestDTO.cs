using System.ComponentModel.DataAnnotations;

namespace DTOs.Subject;

public record SubjectCreateRequestDTO(
    [Required(ErrorMessage = "O nome é obrigatório")] 
    [StringLength(100, MinimumLength = 1, ErrorMessage = "O nome deve ter entre 1 e 100 caracteres")] 
    string Name,
    bool Enabled = false);