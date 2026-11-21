using System.ComponentModel.DataAnnotations;

namespace DTOs.Account;

/// <summary>Exclusão permanente de uma conta. <see cref="ConfirmName"/> precisa ser o nome de usuário da conta.</summary>
public record AdminAccountDeleteRequestDTO(
    [Required(ErrorMessage = "O ID do usuário é obrigatório")]
    string Id,
    [Required(ErrorMessage = "Digite o nome do usuário para confirmar a exclusão")]
    string ConfirmName);
