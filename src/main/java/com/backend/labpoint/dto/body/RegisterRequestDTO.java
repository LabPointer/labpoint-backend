package com.backend.labpoint.dto.body;

import com.backend.labpoint.domain.users.UserRole;
import jakarta.validation.constraints.NotNull;

public record RegisterRequestDTO(@NotNull String username, @NotNull String email, @NotNull String registration, @NotNull String password, @NotNull UserRole role) {
}
