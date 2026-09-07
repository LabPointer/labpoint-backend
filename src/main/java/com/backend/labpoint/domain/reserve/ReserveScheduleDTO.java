package com.backend.labpoint.domain.reserve;

import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;

import java.util.List;

public record ReserveScheduleDTO(@NotNull ReserveSummaryDTO reserve, @NotEmpty List<SchedulesEnum> schedules) {
}
