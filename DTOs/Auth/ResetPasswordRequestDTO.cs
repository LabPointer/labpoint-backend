using System.ComponentModel.DataAnnotations;

namespace DTOs.Auth;

public record ResetPasswordRequestDTO(
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "O formato do e-mail é inválido.")]
    string Email,
    [Required(ErrorMessage = "O token é obrigatório.")]
    string Token,
    [Required(ErrorMessage = "A nova senha é obrigatória.")]
    [StringLength(128, MinimumLength = 6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
    string NewPassword);
