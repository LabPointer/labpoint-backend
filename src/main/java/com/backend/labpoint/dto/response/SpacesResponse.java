package com.backend.labpoint.dto.response;

import com.backend.labpoint.entity.SpaceResources;
import com.backend.labpoint.entity.Spaces;
import lombok.Getter;
import lombok.Setter;

import java.util.List;

@Getter
@Setter
public class SpacesResponse {
    private Spaces space;
    private List<SpaceResources> resources;
}
