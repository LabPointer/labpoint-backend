package com.backend.labpoint.dto.space;

import jakarta.validation.constraints.NotEmpty;

import java.util.Set;

public record DeleteSpaceDTO(@NotEmpty Set<Integer> spaceIds) {
}
