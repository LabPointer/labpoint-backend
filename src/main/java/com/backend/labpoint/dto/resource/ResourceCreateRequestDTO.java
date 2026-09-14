package com.backend.labpoint.dto.resource;

import jakarta.validation.constraints.NotBlank;

public record ResourceCreateRequestDTO(@NotBlank String name) {

}
