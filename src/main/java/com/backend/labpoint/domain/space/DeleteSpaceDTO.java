package com.backend.labpoint.domain.space;

import jakarta.validation.constraints.NotEmpty;

import java.util.Set;

public record DeleteSpaceDTO(@NotEmpty Set<Integer> spaceIds) {
}
