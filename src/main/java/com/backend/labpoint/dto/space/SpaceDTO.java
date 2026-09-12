package com.backend.labpoint.dto.space;

import com.backend.labpoint.dto.resource.ResourceDTO;
import com.backend.labpoint.dto.subject.SubjectDTO;
import com.backend.labpoint.entities.resource.Resource;
import com.backend.labpoint.entities.subject.Subject;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;

import java.util.List;

public record SpaceDTO(
        @NotNull
        Long id,
        @NotBlank
        String name,
        @NotNull
        int capacity,
        String description,
        List<ResourceDTO> resources,
        List<SubjectDTO> subjects,
        boolean locked
) {
}
