package com.backend.labpoint.domain.user;

import jakarta.validation.constraints.NotNull;

public record LoginRequestDTO(@NotNull String registration, @NotNull String password) {
}
