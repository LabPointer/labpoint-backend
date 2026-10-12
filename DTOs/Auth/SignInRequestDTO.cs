using System.ComponentModel.DataAnnotations;

namespace DTOs.Auth;

public record SignInRequestDTO(
    [Required(ErrorMessage = "A matrícula é obrigatória.")]
    [RegularExpression(@"^[0-9]+$", ErrorMessage = "A matrícula deve conter apenas números.")]
    string Registration,
    [Required(ErrorMessage = "A senha é obrigatória.")]
    string Password,
    bool RememberMe = false
);
