package com.backend.labpoint.domain.user;

import jakarta.validation.constraints.NotNull;

import java.util.UUID;

public record UserUpdateResponseDTO(
        UUID id,
        @NotNull String registration,
        @NotNull String username,
        @NotNull String email,
        @NotNull UserRole role,
        Boolean enabled) {

}
