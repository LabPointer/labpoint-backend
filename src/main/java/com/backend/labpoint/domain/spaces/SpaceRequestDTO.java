package com.backend.labpoint.domain.spaces;

import java.util.Set;

public record SpaceRequestDTO(String name, Integer capacity, Set<String> resources) {
}
