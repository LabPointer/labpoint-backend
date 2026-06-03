package com.backend.labpoint.domain.reserve;

import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;

import java.time.LocalDate;
import java.util.Set;

public record CreateReserveRequestDTO(
    @NotEmpty(message = "O campo datas nao pode ser nulo ou vazio!") Set<LocalDate> dates,
    @NotEmpty(message = "O campo schedules nao pode ser nulo ou vazio!") Set<SchedulesEnum> schedules,
    boolean lock) {
}
