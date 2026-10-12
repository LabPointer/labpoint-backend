using System.ComponentModel.DataAnnotations;
using Models;

namespace DTOs.Account;

public record AccountEditRequestDTO([Required(ErrorMessage = "O novo nome de usuário é obrigatório.")] string Username);
