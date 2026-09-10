package com.backend.labpoint.dto.space;

import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;

public record CreateSpaceResponseDTO(
        @NotNull boolean success,
        @NotEmpty String message) {

}
