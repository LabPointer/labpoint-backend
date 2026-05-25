package com.backend.labpoint.domain.space;

import org.springframework.data.repository.query.Param;

import java.util.Set;

public record SpaceRequestDTO(String name, Integer capacity, Set<Integer> resources, Set<Integer> subjects, Integer offset, Integer limit) {
    public SpaceRequestDTO {
        if (name == null) name = "";
        if (resources == null) resources = Set.of();
        if (subjects == null) subjects = Set.of();
        if (offset == null || offset < 0) offset = 0;
        if (limit == null || limit < 10) limit = 10;
        if (limit > 50) limit = 50;
    }
}
