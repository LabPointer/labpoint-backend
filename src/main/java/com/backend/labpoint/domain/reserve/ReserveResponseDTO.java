package com.backend.labpoint.domain.reserve;

import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;

import java.time.LocalDate;
import java.util.List;

public record ReserveResponseDTO(
        @NotNull LocalDate reservedDate,

        @NotEmpty List<Reserve> reserves) {
}
