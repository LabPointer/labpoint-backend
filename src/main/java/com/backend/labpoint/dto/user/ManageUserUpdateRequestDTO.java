package com.backend.labpoint.dto.user;

import com.backend.labpoint.entities.account.AccountRole;

public record ManageUserUpdateRequestDTO(
        String registration,
        String username,
        AccountRole role,
        Boolean enabled
) {
}
