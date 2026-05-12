package com.backend.labpoint.domain.reserves;

import java.time.LocalDate;
import java.util.Set;

public record ReserveRequestDTO(LocalDate date, Set<SchedulesEnum> schedules) {
}
