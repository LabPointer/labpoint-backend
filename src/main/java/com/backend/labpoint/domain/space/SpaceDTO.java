package com.backend.labpoint.domain.space;

import com.backend.labpoint.domain.resource.Resource;
import com.backend.labpoint.domain.subject.Subject;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;

import java.util.List;

public record SpaceDTO(
        @NotNull
        int id,
        @NotBlank
        String name,
        @NotNull
        int capacity,
        String description,
        List<Resource> resources,
        List<Subject> subjects,
        boolean locked
) {
}
