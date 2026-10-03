using System.ComponentModel.DataAnnotations;
using Models;

namespace DTOs.SpaceReserve;

public record SpaceReserveRequestDTO([Required] DateOnly StartAt, [Required] DateOnly EndAt, HashSet<long> SpaceIds, EReserveStatus? Status);