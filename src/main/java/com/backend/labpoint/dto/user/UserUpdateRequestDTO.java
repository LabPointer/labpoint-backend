package com.backend.labpoint.dto.user;

import com.backend.labpoint.entities.user.UserRole;

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
