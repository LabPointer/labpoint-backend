using System.ComponentModel.DataAnnotations;

namespace DTOs.Common;

/// <summary>
/// Exclusão permanente feita pela coordenação. <see cref="ConfirmName"/> precisa ser exatamente o nome do item
/// (para horários, o rótulo "HH:mm - HH:mm"), como proteção contra exclusões acidentais.
/// </summary>
public record AdminDeleteRequestDTO(
    [Required(ErrorMessage = "O ID é obrigatório")]
    long Id,
    [Required(ErrorMessage = "Digite o nome do item para confirmar a exclusão")]
    string ConfirmName);
