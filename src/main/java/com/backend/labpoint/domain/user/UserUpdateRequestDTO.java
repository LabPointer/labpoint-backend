package com.backend.labpoint.domain.user;

import jakarta.validation.constraints.NotNull;

import java.util.UUID;

public record UserUpdateRequestDTO(
        UUID uuid,
        String registration,
        String username,
        String email,
        String password,
        UserRole role,
        Boolean enabled
) {
}
