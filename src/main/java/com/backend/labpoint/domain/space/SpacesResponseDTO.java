package com.backend.labpoint.domain.space;

import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;

import java.util.List;

public record SpacesResponseDTO(
        @NotEmpty List<SpaceDTO> spaces,
        @NotNull int offset,
        @NotNull int limit,
        @NotNull int total
) {

}
