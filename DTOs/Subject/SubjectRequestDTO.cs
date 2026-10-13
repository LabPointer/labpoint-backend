using System.ComponentModel.DataAnnotations;

namespace DTOs.Subject;

public record SubjectRequestDTO(
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Nome precisa conter pelo menos 1 caracter")]
    string? Name,
    bool? Enabled, 
    [Range(10, int.MaxValue, ErrorMessage = "O limite deve ser maior ou igual a 10")]
    int Limit = 10, 
    [Range(0, int.MaxValue, ErrorMessage = "O deslocamento deve ser maior ou igual a 0")]
    int Offset = 0);