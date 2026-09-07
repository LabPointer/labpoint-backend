package com.backend.labpoint.domain.reserve;

import java.time.LocalDate;

import jakarta.validation.constraints.NotEmpty;

public record ExistingScheduleRequestDTO(
    @NotEmpty(message = "O campo data nao pode ser nulo ou vazio!") LocalDate dateFrom,
    @NotEmpty(message = "O campo data nao pode ser nulo ou vazio!") LocalDate dateTo) {

}
