package com.backend.labpoint.domain.user;

import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;

public record LoginResponseDTO(
        @NotEmpty String username,
        @NotEmpty String role,
        @NotNull long tokenExpireIn) {

}
