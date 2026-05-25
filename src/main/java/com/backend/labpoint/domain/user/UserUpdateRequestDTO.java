package com.backend.labpoint.domain.user;

import jakarta.validation.constraints.NotNull;

import java.util.UUID;

public record UserUpdateRequestDTO(
        UUID uuid,
        String registration,
        String email,
        String password,
        String role,
        Boolean enabled
        ) {
}
