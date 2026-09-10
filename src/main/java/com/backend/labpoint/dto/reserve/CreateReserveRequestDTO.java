package com.backend.labpoint.dto.reserve;

import com.backend.labpoint.entities.schedule.SchedulesEnum;
import jakarta.validation.constraints.NotEmpty;

import java.time.LocalDate;
import java.util.Set;

public record CreateReserveRequestDTO(
        @NotEmpty(message = "O campo data nao pode ser nulo ou vazio!") LocalDate dateFrom,
        @NotEmpty(message = "O campo data nao pode ser nulo ou vazio!") LocalDate dateTo,
        @NotEmpty(message = "O campo schedules nao pode ser nulo ou vazio!") Set<SchedulesEnum> schedules,
        @NotEmpty(message = "O campo purpose nao pode ser nulo ou vazio!") String purpose,
        Boolean lock) {
}
