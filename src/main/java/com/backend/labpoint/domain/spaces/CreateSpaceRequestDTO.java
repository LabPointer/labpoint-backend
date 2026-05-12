package com.backend.labpoint.domain.spaces;

import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;

import java.util.Set;

public record CreateSpaceRequestDTO(
        @NotNull(message = "Name is required")
        @Size(min = 4, max = 64, message = "Name must be between 4 and 64 characters")
        String name,

        @Size(max = 500, message = "Description cannot exceed 500 characters")
        String description,

        @NotNull(message = "Capacity is required")
        @Min(value = 1, message = "Capacity must be at least 1")
        Integer capacity,

        @NotNull(message = "Resource is required")
        Set<String> resources) {
}
