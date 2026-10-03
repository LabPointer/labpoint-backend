using System.ComponentModel.DataAnnotations;
using Models;

namespace DTOs.SpaceReserve;

public record SpaceReserveRequestDTO(
    [Required(ErrorMessage = "A data de início da reserva é obrigatória")] 
    DateOnly StartAt, 
    [Required(ErrorMessage = "A data de término da reserva é obrigatória")] 
    DateOnly EndAt,
    HashSet<long>? SpaceIds, 
    EReserveStatus? Status);