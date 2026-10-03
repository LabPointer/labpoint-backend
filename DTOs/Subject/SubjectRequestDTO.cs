namespace DTOs.Subject;

public record SubjectRequestDTO(string Name, bool IsActive, int Limit = 10, int Offset = 0);