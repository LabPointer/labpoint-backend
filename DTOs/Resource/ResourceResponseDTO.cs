using System.ComponentModel.DataAnnotations;

namespace DTOs.Resource;

public record ResourceResponseDTO(long Id, string Name, bool CanReserve, bool Enabled);