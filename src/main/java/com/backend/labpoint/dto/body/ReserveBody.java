package com.backend.labpoint.dto.body;

import com.backend.labpoint.entity.SchedulesEnum;

import java.time.LocalDate;
import java.util.Set;

public record ReserveBody(LocalDate date, Set<SchedulesEnum> schedules){}
