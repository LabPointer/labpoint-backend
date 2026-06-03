package com.backend.labpoint.domain.user;

import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;

import java.util.UUID;

public record UserUpdateResponseDTO(
        UUID id,
        @NotEmpty String registration,
        @NotEmpty String username,
        @NotEmpty String email,
        @NotNull UserRole role,
        Boolean enabled) {

}
