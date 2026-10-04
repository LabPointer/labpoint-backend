namespace DTOs.SpaceReserve;

public record SpaceReserveEditRequestDTO(
    DateOnly? StartAt,
    DateOnly? EndAt,
    HashSet<long>? ScheduleIds);