package com.backend.labpoint.domain.spaces;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import lombok.Getter;
import lombok.Setter;

import java.util.List;

@Getter
@Setter
public class SpacesResponseDTO {
    @NotNull
    private Integer id;

    @NotBlank
    private String name;

    @NotNull
    private Integer capacity;

    private List<String> resources;

    public void setSpaceResponse(Spaces space) {
        this.id = space.getId();
        this.name = space.getName();
        this.capacity = space.getCapacity();
    }
}
