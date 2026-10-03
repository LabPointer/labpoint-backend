using DTOs.Schedule;
using DTOs.Space;
using Models;

namespace DTOs.SpaceReserve;

public record SpaceReserveResponseDTO(
    long Id,
    DateTimeOffset CreatedAt,
    DateOnly DateFrom,
    DateOnly DateTo,
    string Purpose,
    EReserveStatus Status,
    SpaceResponseDTO Spaces,
    List<ScheduleResponseDTO> Schedules);