package com.backend.labpoint.domain.space;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import lombok.Getter;
import lombok.Setter;

import java.util.List;

@Getter
@Setter
public class SpaceDTO {
    @NotNull
    private Integer id;

    @NotBlank
    private String name;

    @NotNull
    private Integer capacity;

    private List<String> resources;

    private List<String> subjects;
}
