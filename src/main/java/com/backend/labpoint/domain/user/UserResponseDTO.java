package com.backend.labpoint.domain.user;

import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;

import java.util.UUID;

public record UserResponseDTO(
        @NotNull UUID uuid,
        @NotEmpty String registration,
        @NotEmpty String username,
        @NotEmpty String email,
        @NotEmpty UserRole role,
        @NotNull boolean enabled,
        @NotNull int offset,
        @NotNull int limit,
        @NotNull int total
) {
}
