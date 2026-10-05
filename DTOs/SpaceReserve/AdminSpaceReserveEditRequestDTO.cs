using System.ComponentModel.DataAnnotations;
using Models;

namespace DTOs.SpaceReserve;

public record AdminSpaceReserveEditRequestDTO([Required(ErrorMessage = "O status da reserva é obrigatório.")] EReserveStatus Status);