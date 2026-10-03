namespace DTOs.Error;

public record ErroResponseDTO(string Message, int StatusCode, bool Logout = false);