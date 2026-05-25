package com.backend.labpoint.domain.space;

import java.util.Set;

public record PatchSpaceRequestDTO(
        String name,
        Integer capacity,
        Set<String> resources,
        Set<String> subjects) {
    public PatchSpaceRequestDTO {
        if (resources == null) resources = Set.of();
        if (subjects == null) subjects = Set.of();
    }
}
