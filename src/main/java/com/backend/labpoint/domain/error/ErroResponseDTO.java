package com.backend.labpoint.domain.error;

import jakarta.validation.constraints.NotEmpty;

public record ErroResponseDTO(
        @NotEmpty
        String message) {

}
