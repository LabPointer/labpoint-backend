using System.ComponentModel.DataAnnotations;
using Models;

namespace DTOs.Account;

public record AdminAccountRequestDTO(
    [StringLength(16, MinimumLength = 1, ErrorMessage = "O registro deve ter no mínimo 1 e no máximo 16 caracteres")]
    string? Registration,
    [StringLength(100, MinimumLength = 1, ErrorMessage = "O nome de usuário deve ter no mínimo 1 e no máximo 100 caracteres")]
    string? Username,
    [StringLength(320, MinimumLength = 1, ErrorMessage = "O e-mail deve ter no mínimo 1 e no máximo 320 caracteres")]
    [EmailAddress(ErrorMessage = "O e-mail informado não é válido")]
    string? Email,
    EAccountRole? Role,
    [Range(10, int.MaxValue, ErrorMessage = "O limite de resultados por página deve ser maior que 10")]
    int Limit = 10,
    int Page = 0);