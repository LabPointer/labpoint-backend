package com.backend.labpoint.dto.subject;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;

public record SubjectDTO(@NotNull Long id, @NotBlank String name) {
}
