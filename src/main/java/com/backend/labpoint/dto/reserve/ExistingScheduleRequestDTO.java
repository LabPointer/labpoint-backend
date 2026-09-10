package com.backend.labpoint.dto.reserve;

import jakarta.validation.constraints.NotEmpty;

import java.time.LocalDate;

public record ExistingScheduleRequestDTO(
        @NotEmpty(message = "O campo data nao pode ser nulo ou vazio!") LocalDate dateFrom,
        @NotEmpty(message = "O campo data nao pode ser nulo ou vazio!") LocalDate dateTo) {

}
