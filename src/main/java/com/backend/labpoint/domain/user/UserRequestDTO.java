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
        if (limit == null) limit = 10;
    }
}
