using System.ComponentModel.DataAnnotations;
using Models;

namespace DTOs.SpaceReserve;

public record AdminSpaceReserveRequestDTO(
    [Required(ErrorMessage = "A data de início da reserva é obrigatória")] 
    DateOnly StartAt, 
    [Required(ErrorMessage = "A data de término da reserva é obrigatória")] 
    DateOnly EndAt,
    [MinLength(1, ErrorMessage = "É necessário informar pelo menos um espaço para a reserva")]
    HashSet<long>? SpaceIds, 
    [MinLength(1, ErrorMessage = "É necessário informar pelo menos um horário para a reserva")]
    HashSet<long>? ScheduleIds,
    EReserveStatus? Status,
    [MinLength(1, ErrorMessage = "É necessário informar pelo menos uma conta para a reserva")]
    HashSet<string>? AccountIds);