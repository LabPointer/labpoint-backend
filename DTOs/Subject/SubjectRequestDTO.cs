using System.ComponentModel.DataAnnotations;

namespace DTOs.Subject;

public record SubjectRequestDTO(
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Nome precisa conter pelo menos 1 caracter")]
    string? Name,
    bool IsActive = true, 
    int Limit = 10, 
    int Offset = 0);