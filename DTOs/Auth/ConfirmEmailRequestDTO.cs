using System.ComponentModel.DataAnnotations;

namespace DTOs.Auth;

public record ConfirmEmailRequestDTO(string UserId, string Token);