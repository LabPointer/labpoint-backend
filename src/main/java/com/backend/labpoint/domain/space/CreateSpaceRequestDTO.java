package com.backend.labpoint.domain.space;

import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;

import java.util.Set;

public record CreateSpaceRequestDTO(
        @NotEmpty(message = "Campo name nao pode ser nulo ou vazio!")
        @Size(min = 4, max = 64, message = "Name must be between 4 and 64 characters")
        String name,

        @Size(max = 128, message = "Campo description nao pode ter mais de 128 caracteres")
        String description,

        @NotNull(message = "Campo capacity nao pode ser nulo!")
        @Min(value = 1, message = "Campo capacity deve ser no minimo 1 ou mais!")
        Integer capacity,

        Set<Integer> resources,

        Set<Integer> subjects
) {
    public CreateSpaceRequestDTO {
        if (resources == null) resources = Set.of();
        if (subjects == null) subjects = Set.of();
    }
}
