package com.backend.labpoint.domain.user;

import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotNull;

public record RegisterRequestDTO(
        @NotNull String username,
        @NotNull @Email String email,
        @NotNull String registration,
        @NotNull String password,
        @NotNull UserRole role,
        Boolean enabled) {
}
