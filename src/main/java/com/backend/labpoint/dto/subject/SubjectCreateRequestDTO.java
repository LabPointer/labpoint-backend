package com.backend.labpoint.dto.subject;

import jakarta.validation.constraints.NotBlank;

public record SubjectCreateRequestDTO(@NotBlank String name) {

}
