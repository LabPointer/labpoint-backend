package com.backend.labpoint.domain.space;

import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;

import java.util.List;

public record PatchSpaceResponseDTO(
        @NotNull int id,
        @NotEmpty String name,
        int capacity,
        List<Integer> resources,
        List<Integer> subjects) {
}
