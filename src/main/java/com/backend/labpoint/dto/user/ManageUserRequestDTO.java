package com.backend.labpoint.dto.user;

import com.backend.labpoint.entities.account.AccountRole;

import jakarta.validation.constraints.Email;

public record ManageUserRequestDTO(
        String registration,
        String username,
        @Email String email,
        AccountRole role,
        Integer offset,
        Integer limit
) {
    public ManageUserRequestDTO {
        if (offset == null) offset = 0;
        offset = offset < 0 ? 0 : offset;
        if (limit == null) limit = 20;
        limit = limit < 20 ? 20 :
                limit > 50 ? 50 : limit;
    }
}
