using DTOs.Account;
using DTOs.Resource;
using DTOs.Schedule;
using DTOs.Usage;
using Models;

namespace DTOs.ResourceReserve;

/// <summary>Reserva de sala a que uma reserva de recurso está vinculada.</summary>
public record LinkedSpaceReserveDTO(long Id, long SpaceId, string SpaceName, EReserveStatus Status);

/// <param name="SpaceReserve">Nulo quando a reserva é avulsa.</param>
public record ResourceReserveResponseDTO(
    long Id,
    DateTimeOffset CreatedAt,
    DateOnly DateFrom,
    DateOnly DateTo,
    string Purpose,
    EReserveStatus Status,
    int Quantity,
    ResourceResponseDTO Resource,
    List<ScheduleResponseDTO> Schedules,
    LinkedSpaceReserveDTO? SpaceReserve,
    UsageSummaryDTO? Usage = null
);

public record AdminResourceReserveResponseDTO(
    long Id,
    DateTimeOffset CreatedAt,
    DateOnly DateFrom,
    DateOnly DateTo,
    string Purpose,
    EReserveStatus Status,
    int Quantity,
    ResourceResponseDTO Resource,
    List<ScheduleResponseDTO> Schedules,
    LinkedSpaceReserveDTO? SpaceReserve,
    AccountResponseDTO Account,
    UsageSummaryDTO? Usage = null
);
