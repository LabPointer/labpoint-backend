package com.backend.labpoint.dto.user;

import com.backend.labpoint.entities.account.AccountRole;

public record ManageUserUpdateRequestDTO(
        String registration,
        String username,
        String email,
        String password,
        AccountRole role,
        Boolean enabled
) {
}
