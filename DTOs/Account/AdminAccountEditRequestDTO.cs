using System.ComponentModel.DataAnnotations;
using Models;

namespace DTOs.Account;

public record AdminAccountEditRequestDTO(
    [Required(ErrorMessage = "Id é obrigatorio")]
    string Id,
    [StringLength(100, MinimumLength = 1, ErrorMessage = "O nome de usuário deve ter no mínimo 1 e no máximo 100 caracteres")]
    string? Registration,
    [StringLength(100, MinimumLength = 1, ErrorMessage = "O nome de usuário deve ter no mínimo 1 e no máximo 100 caracteres")]
    string? Username,
    EAccountRole? Role,
    bool? LockAccount);
