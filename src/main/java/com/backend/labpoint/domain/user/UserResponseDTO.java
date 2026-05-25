package com.backend.labpoint.domain.user;

import jakarta.validation.constraints.NotNull;

import java.util.UUID;

public record UserResponseDTO(
        @NotNull UUID uuid,
        @NotNull String registration,
        @NotNull String username,
        @NotNull String email,
        @NotNull UserRole role,
        boolean enabled,
        int offset,
        int limit,
        int total
        ) {
}
