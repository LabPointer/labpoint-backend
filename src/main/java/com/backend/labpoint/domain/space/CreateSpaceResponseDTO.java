package com.backend.labpoint.domain.space;

import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;

public record CreateSpaceResponseDTO(
    @NotNull boolean success,
    @NotEmpty String message) {

}
