using System.ComponentModel.DataAnnotations;
using Models;

namespace DTOs.Account;

public record AdminAccountRequestDTO(
    [MinLength(1, ErrorMessage = "A busca deve ter no mínimo 1 caractere")]
    string? SearchQuery,
    EAccountRole? Role,
    [Range(10, int.MaxValue, ErrorMessage = "O limite de resultados por página deve ser maior que 10")]
    int Limit = 10,
    int Page = 0);