package com.backend.labpoint.dto.reserve;

import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;

import java.time.LocalDate;
import java.util.List;

public record ReserveResponseDTO(
        @NotNull LocalDate reservedDate,

        @NotEmpty List<ReserveDTO> reserves) {
}
