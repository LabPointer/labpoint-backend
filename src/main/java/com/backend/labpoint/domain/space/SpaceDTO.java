package com.backend.labpoint.domain.space;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import lombok.Getter;
import lombok.Setter;

import java.util.List;

public record SpaceDTO(
    @NotNull
    int id,
    @NotBlank
    String name,
    @NotNull
    int capacity,
    List<Integer> resources,
    List<Integer> subjects
){}
