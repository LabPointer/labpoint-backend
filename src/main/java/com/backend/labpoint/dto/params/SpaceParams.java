package com.backend.labpoint.dto.params;

import java.util.Set;

import com.backend.labpoint.entity.ResourcesEnum;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.Setter;

public record SpaceParams(String name, Integer capacity, Set<ResourcesEnum> resources){}
