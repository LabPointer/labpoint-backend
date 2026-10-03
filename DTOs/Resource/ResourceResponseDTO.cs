using System.ComponentModel.DataAnnotations;

namespace DTOs.Resource;

public record ResourceResponseDTO(long Id, string Name, string Description, bool CanReserve, bool Enabled);