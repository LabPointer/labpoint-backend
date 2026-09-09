package com.backend.labpoint.domain.reserve;

import jakarta.validation.constraints.NotNull;

import java.time.LocalDate;

public record ReserveDateDTO(@NotNull LocalDate dateFrom, @NotNull LocalDate dateTo) {
}
