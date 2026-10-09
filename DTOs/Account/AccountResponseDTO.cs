using Models;

namespace DTOs.Account;

public record AccountResponseDTO(
    string Id,
    string Registration,
    string Username,
    string Email,
    EAccountRole Role,
    bool Enabled);