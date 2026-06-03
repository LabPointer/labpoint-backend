package com.backend.labpoint.domain.reserve;

import jakarta.validation.constraints.NotEmpty;

import java.util.Set;

public record DeleteReserveRequestDTO(@NotEmpty(message = "O array de reserveIds nao pode ser nulo ou vazio!") Set<Integer> reserveIds) {
}
