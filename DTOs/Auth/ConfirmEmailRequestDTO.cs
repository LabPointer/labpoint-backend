using System.ComponentModel.DataAnnotations;

namespace DTOs.Auth;

public record ConfirmEmailRequestDTO(
    [Required(ErrorMessage = "User ID é obrigatório")] 
    string UserId, 
    [Required(ErrorMessage = "Token é obrigatório")] 
    string Token
    );