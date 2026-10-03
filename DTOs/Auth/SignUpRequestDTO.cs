using System.ComponentModel.DataAnnotations;
using Models;

namespace DTOs.Auth;

public record SignUpRequestDTO(
    [Required(ErrorMessage = "O nome de usuário é obrigatório.")]
    [StringLength(60, MinimumLength = 4, ErrorMessage = "O usuário deve ter entre 4 e 64 caracteres.")]
    string Username, 
    [Required(ErrorMessage = "A matricula é obrigatoria.")]
    [StringLength(20, MinimumLength = 1, ErrorMessage = "A matricula deve ter entre 1 e 16 caracteres.")]
    string Registration, 
    [EnumDataType(typeof(EAccountRole), ErrorMessage = "Cargo invalido.")]
    EAccountRole Role, 
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "O formato do e-mail é inválido.")]
    string Email,
    [Required(ErrorMessage = "A senha é obrigatória.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter no mínimo 6 e no maximo 100 caracteres.")]
    string Password);