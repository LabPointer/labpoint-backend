package com.backend.labpoint.dto.response;

import com.backend.labpoint.domain.spaces.SpaceResources;
import com.backend.labpoint.domain.spaces.Spaces;
import jakarta.validation.constraints.NotEmpty;
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
