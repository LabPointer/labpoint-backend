package com.backend.labpoint.domain.user;

public record UserRequestDTO(
        String registration,
        String username,
        String email,
        UserRole role,
        Integer offset,
        Integer limit
) {
    public UserRequestDTO {
        if (offset == null) offset = 0;
        offset = offset < 0 ? 0 : offset;
        if (limit == null) limit = 20;
        limit = limit < 20 ? 20 :
                limit > 50 ? 50: limit;
    }
}
