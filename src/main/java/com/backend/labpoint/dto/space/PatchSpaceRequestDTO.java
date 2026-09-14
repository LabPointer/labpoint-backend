package com.backend.labpoint.dto.space;

import java.util.Set;

public record PatchSpaceRequestDTO(
        String name,
        Integer capacity,
        Set<Long> resources,
        Set<Long> subjects) {
    public PatchSpaceRequestDTO {
        if (resources == null) resources = Set.of();
        if (subjects == null) subjects = Set.of();
    }
}
