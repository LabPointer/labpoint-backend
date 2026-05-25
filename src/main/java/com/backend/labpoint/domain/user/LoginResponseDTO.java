package com.backend.labpoint.domain.user;

import jakarta.validation.constraints.NotNull;

public record LoginResponseDTO(@NotNull String username, @NotNull String role, long tokenExpireIn) {

}
