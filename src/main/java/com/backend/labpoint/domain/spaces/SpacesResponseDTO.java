package com.backend.labpoint.domain.spaces;

import jakarta.validation.constraints.NotNull;
import lombok.Getter;
import lombok.Setter;

import java.util.List;

@Getter
@Setter
public class SpacesResponseDTO {
    @NotNull
    private Spaces space;

    private List<SpaceResources> resources;
}
