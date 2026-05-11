package com.backend.labpoint.dto.body;

import jakarta.validation.constraints.NotNull;

public record AuthRequestDTO(@NotNull String registration, @NotNull String password) {}
