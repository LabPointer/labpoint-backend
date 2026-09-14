package com.backend.labpoint.dto.subject;

import jakarta.validation.constraints.NotBlank;

public record SubjectUpdateRequestDTO(@NotBlank String name) {

}
