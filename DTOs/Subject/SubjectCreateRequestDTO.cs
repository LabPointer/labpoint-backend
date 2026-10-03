using System.ComponentModel.DataAnnotations;

namespace DTOs.Subject;

public record SubjectCreateRequestDTO([Required] [StringLength(100, MinimumLength = 1)] string Name, bool Enabled = false);