using System.ComponentModel.DataAnnotations;

namespace DTOs.Subject;

public record SubjectEditRequestDTO([Required] long Id, [Required][StringLength(100, MinimumLength = 1)] string Name, bool Enabled = true);