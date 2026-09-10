package com.backend.labpoint.dto.reserve;

import jakarta.validation.constraints.NotNull;

import java.time.LocalDate;

public record ReserveDateDTO(@NotNull LocalDate dateFrom, @NotNull LocalDate dateTo) {
}
