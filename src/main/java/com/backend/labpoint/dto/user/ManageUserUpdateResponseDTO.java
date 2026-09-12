package com.backend.labpoint.dto.user;

import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;

import java.util.UUID;

import com.backend.labpoint.entities.account.AccountRole;

public record ManageUserUpdateResponseDTO(
        UUID id,
        @NotEmpty String registration,
        @NotEmpty String username,
        @NotEmpty String email,
        @NotNull AccountRole role,
        Boolean enabled) {

}
