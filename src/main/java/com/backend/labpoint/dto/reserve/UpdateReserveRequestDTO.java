package com.backend.labpoint.dto.reserve;

import com.backend.labpoint.entities.schedule.SchedulesEnum;

import java.time.LocalDate;

public record UpdateReserveRequestDTO(
        LocalDate reservedDate,
        SchedulesEnum schedule,
        Boolean lock,
        String userRegistration,
        Integer spaceId) {
}
