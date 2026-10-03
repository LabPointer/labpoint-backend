using System.ComponentModel.DataAnnotations;

namespace DTOs.Auth;

public record ResetPasswordRequestDTO(
    [property: Required(ErrorMessage = "O e-mail é obrigatório.")]
    [property: EmailAddress(ErrorMessage = "O formato do e-mail é inválido.")]
    string Email,
    [property: Required(ErrorMessage = "O token é obrigatório.")]
    string Token,
    [property: Required(ErrorMessage = "A nova senha é obrigatória.")]
    [property: StringLength(128, MinimumLength = 6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
    string NewPassword);
