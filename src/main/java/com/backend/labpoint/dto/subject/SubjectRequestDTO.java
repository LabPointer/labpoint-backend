package com.backend.labpoint.dto.subject;

import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;

public record SubjectRequestDTO(@NotBlank String name, @Min(10) Integer limit, @Min(0) Integer offset) {

}
