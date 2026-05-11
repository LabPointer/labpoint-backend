package com.backend.labpoint.dto.body;

import com.backend.labpoint.domain.reserves.SchedulesEnum;

import java.time.LocalDate;
import java.util.Set;

public record ReservRequestDTO(LocalDate date, Set<SchedulesEnum> schedules){}
