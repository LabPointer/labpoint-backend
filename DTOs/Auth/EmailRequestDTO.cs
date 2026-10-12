using System.ComponentModel.DataAnnotations;

namespace DTOs.Auth;

/// <summary>Usado em forgot-password e resend-confirmation.</summary>
public record EmailRequestDTO(
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "O formato do e-mail é inválido.")]
    string Email);
