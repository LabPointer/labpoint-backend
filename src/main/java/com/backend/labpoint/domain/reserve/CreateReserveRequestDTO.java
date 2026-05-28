package com.backend.labpoint.domain.reserve;

import jakarta.validation.constraints.NotEmpty;

import java.time.LocalDate;
import java.util.Set;

public record CreateReserveRequestDTO(@NotEmpty Set<LocalDate> dates, @NotEmpty Set<SchedulesEnum> schedules,
                                      Boolean lock) {
}
