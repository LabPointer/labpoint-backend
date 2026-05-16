package com.backend.labpoint.domain.users;

import jakarta.validation.constraints.NotNull;

public record LoginResponseDTO(@NotNull String username, @NotNull String role, long tokenExpireIn) {

}
