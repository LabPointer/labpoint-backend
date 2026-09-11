package com.backend.labpoint.dto.user;

import com.backend.labpoint.entities.user.UserRole;

public record ManageUserUpdateRequestDTO(
        String registration,
        String username,
        String email,
        String password,
        UserRole role,
        Boolean enabled
) {
}
