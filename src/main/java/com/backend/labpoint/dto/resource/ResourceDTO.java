package com.backend.labpoint.dto.resource;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;

public record ResourceDTO(@NotNull Long id, @NotBlank String name) {
}
