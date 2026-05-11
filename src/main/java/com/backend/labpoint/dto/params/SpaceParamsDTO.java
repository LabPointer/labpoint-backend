package com.backend.labpoint.dto.params;

import java.util.Set;

import com.backend.labpoint.domain.spaces.ResourcesEnum;

public record SpaceParamsDTO(String name, Integer capacity, Set<ResourcesEnum> resources){}
