using DTOs.Schedule;
using DTOs.Space;
using DTOs.Usage;
using Models;

namespace DTOs.SpaceReserve;

/// <param name="Usage">Contagem das sessões de uso (check-in/check-out) da reserva.</param>
public record SpaceReserveResponseDTO(
    long Id,
    DateTimeOffset CreatedAt,
    DateOnly DateFrom,
    DateOnly DateTo,
    string Purpose,
    EReserveStatus Status,
    SpaceResponseDTO Space,
    List<ScheduleResponseDTO> Schedules,
    List<LinkedResourceDTO> Resources,
    UsageSummaryDTO? Usage = null
);
