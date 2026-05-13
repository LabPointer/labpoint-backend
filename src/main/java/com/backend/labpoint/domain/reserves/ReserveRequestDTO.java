package com.backend.labpoint.domain.reserves;

import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;

import java.time.LocalDate;
import java.util.Set;

public record ReserveRequestDTO(@NotNull LocalDate date, @NotEmpty Set<SchedulesEnum> schedules) {
}
