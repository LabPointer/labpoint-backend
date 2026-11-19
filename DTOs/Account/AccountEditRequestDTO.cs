using System.ComponentModel.DataAnnotations;

namespace DTOs.Account;

/// <summary>Edição da própria conta. Campos nulos são ignorados (PATCH parcial).</summary>
public record AccountEditRequestDTO(
    [StringLength(100, MinimumLength = 1, ErrorMessage = "O nome de usuário deve ter no mínimo 1 e no máximo 100 caracteres")]
    string? Username,
    long? SubjectId);
