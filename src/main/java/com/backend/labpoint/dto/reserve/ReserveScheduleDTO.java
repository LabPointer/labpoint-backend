package com.backend.labpoint.dto.reserve;

import com.backend.labpoint.entities.schedule.SchedulesEnum;
import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;

import java.util.List;

public record ReserveScheduleDTO(@NotNull ReserveSummaryDTO reserve, @NotEmpty List<SchedulesEnum> schedules) {
}
