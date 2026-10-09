using Models;

namespace DTOs.Schedule;

public record ScheduleRequestDTO(EShift? shift, bool? Enabled);