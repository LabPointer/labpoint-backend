package com.backend.labpoint.dto.resource;

import jakarta.validation.constraints.NotEmpty;

import java.util.Set;

public record DeleteResourceRequestDTO(
        @NotEmpty(message = "O array de resourceIds nao pode ser nulo ou vazio!") Set<Integer> resourceIds) {
}
