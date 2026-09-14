package com.backend.labpoint.dto.resource;

import jakarta.validation.constraints.NotBlank;

public record ResourceUpdateRequestDTO(@NotBlank String name) {

}
