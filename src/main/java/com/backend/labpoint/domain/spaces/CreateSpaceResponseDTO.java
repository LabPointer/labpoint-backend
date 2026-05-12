package com.backend.labpoint.domain.spaces;

import jakarta.validation.constraints.NotNull;

public record CreateSpaceResponseDTO(@NotNull boolean success, String message) {

}
