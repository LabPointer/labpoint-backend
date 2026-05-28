package com.backend.labpoint.domain.reserve;

import jakarta.validation.constraints.NotEmpty;

import java.util.Set;

public record DeleteReserveRequestDTO(@NotEmpty Set<Integer> reserveIds) {
}
