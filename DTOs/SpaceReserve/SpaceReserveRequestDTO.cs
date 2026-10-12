using System.ComponentModel.DataAnnotations;
using Models;

namespace DTOs.SpaceReserve;

public record SpaceReserveRequestDTO(
    [Required(ErrorMessage = "A data de início da reserva é obrigatória")] 
    DateOnly StartAt, 
    [Required(ErrorMessage = "A data de término da reserva é obrigatória")] 
    DateOnly EndAt,
    [MinLength(1, ErrorMessage = "É necessário informar pelo menos um espaço para a reserva")]
    HashSet<long>? SpaceIds,
    EReserveStatus? Status);