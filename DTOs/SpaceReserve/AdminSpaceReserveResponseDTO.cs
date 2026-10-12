using DTOs.Account;
using DTOs.Schedule;
using DTOs.Space;
using Models;

namespace DTOs.SpaceReserve;

public record AdminSpaceReserveResponseDTO(
    long Id,
    DateTimeOffset CreatedAt,
    DateOnly DateFrom,
    DateOnly DateTo,
    string Purpose,
    EReserveStatus Status,
    SpaceResponseDTO Spaces,
    AccountResponseDTO Account,
    List<ScheduleResponseDTO> Schedules);