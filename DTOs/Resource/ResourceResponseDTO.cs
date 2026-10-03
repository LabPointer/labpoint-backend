using System.ComponentModel.DataAnnotations;

namespace DTOs.Resource;

public record ResourceResponseDTO(long Id, [Required] string Name, [Required] string Description, bool CanReserve, bool Enabled);