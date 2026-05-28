package com.backend.labpoint.domain.space;

import jakarta.validation.constraints.NotNull;

import java.util.List;

public record PatchSpaceResponseDTO(
        @NotNull Integer id,
        @NotNull String name,
        @NotNull Integer capacity,
        List<String> resources,
        List<String> subjects) {
}
