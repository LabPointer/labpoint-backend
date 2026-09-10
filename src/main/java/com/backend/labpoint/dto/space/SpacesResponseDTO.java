package com.backend.labpoint.dto.space;

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
