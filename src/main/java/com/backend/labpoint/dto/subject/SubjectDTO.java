package com.backend.labpoint.dto.subject;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;

public record SubjectDTO(@NotNull Integer id, @NotBlank String name) {
}
