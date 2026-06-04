package com.backend.labpoint.domain.subject;

import jakarta.validation.constraints.NotEmpty;

import java.util.Set;

public record DeleteSubjectRequestDTO(
        @NotEmpty(message = "O campo subjectIds nao pode ser nulo ou vazio!") Set<Integer> subjectIds) {
}
