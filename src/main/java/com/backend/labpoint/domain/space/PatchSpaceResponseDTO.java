package com.backend.labpoint.domain.space;

import java.util.List;

import jakarta.validation.constraints.NotNull;

public record PatchSpaceResponseDTO(
        @NotNull Integer id,
        @NotNull String name,
        @NotNull Integer capacity,
        List<String> resources,
        List<String> subjects) {
}
