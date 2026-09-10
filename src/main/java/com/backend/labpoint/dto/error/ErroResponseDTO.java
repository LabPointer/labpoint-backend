package com.backend.labpoint.dto.error;

import jakarta.validation.constraints.NotEmpty;

public record ErroResponseDTO(
        @NotEmpty
        String message,
        boolean logout) {

}
