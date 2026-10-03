using System.ComponentModel.DataAnnotations;

namespace DTOs.Subject;

public record SubjectEditRequestDTO(
    [Required(ErrorMessage = "O ID é obrigatório")] 
    long Id, 
    [StringLength(100, MinimumLength = 1, ErrorMessage = "O nome deve ter entre 1 e 100 caracteres")]
    string? Name, 
    bool? Enabled);