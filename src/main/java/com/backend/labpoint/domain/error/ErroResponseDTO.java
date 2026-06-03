package com.backend.labpoint.domain.error;

import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.media.Schema.RequiredMode;
import jakarta.validation.constraints.NotEmpty;

public record ErroResponseDTO(
    @NotEmpty
    String message) {

}
