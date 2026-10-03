using System.ComponentModel.DataAnnotations;

namespace DTOs.Auth;

public record SignInRequestDTO(
    [Required(ErrorMessage = "A matrícula é obrigatória.")]
    string Registration,
    [Required(ErrorMessage = "A senha é obrigatória.")]
    string Password,
    bool RememberMe = false);
