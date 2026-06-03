package com.backend.labpoint.domain.reserve;

import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;

import java.time.LocalDate;
import java.util.List;

import io.swagger.v3.oas.annotations.media.Schema;

public record ReserveResponseDTO(
    @NotNull LocalDate reservedDate,

    @NotEmpty List<Reserve> reserves) {
}
