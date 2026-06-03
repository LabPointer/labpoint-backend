package com.backend.labpoint.domain.user;

import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.media.Schema.RequiredMode;
import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;

public record LoginResponseDTO(
    @NotEmpty String username,
    @NotEmpty String role,
    @NotNull long tokenExpireIn) {

}
