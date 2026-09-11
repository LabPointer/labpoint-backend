package com.backend.labpoint.dto.user;

import java.time.LocalDate;

import com.backend.labpoint.entities.user.UserRole;
import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;

public record ManageUserResponseDTO(
        @NotEmpty String registration,
        @NotEmpty String username,
        @NotEmpty String email,
        @NotEmpty UserRole role,
        @NotNull boolean enabled,
        @NotNull LocalDate createdAt
) {
}
