package com.backend.labpoint.service;

import java.util.List;
import java.util.Set;

import com.backend.labpoint.domain.spaces.SpaceResources;
import com.backend.labpoint.repository.SpaceResourcesRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import com.backend.labpoint.domain.spaces.ResourcesEnum;
import com.backend.labpoint.domain.spaces.Spaces;
import com.backend.labpoint.repository.SpacesRepository;

@Service
public class SpacesService {
    private final SpacesRepository spaceRepository;

    private final SpaceResourcesRepository spaceResourcesRepository;

    public SpacesService(SpacesRepository spaceRepository, SpaceResourcesRepository spaceResourcesRepository) {
        this.spaceRepository = spaceRepository;
        this.spaceResourcesRepository = spaceResourcesRepository;
    }

    @Transactional(readOnly = true)
    public List<Spaces> getSpaces(String name, Integer capacity) {
        return spaceRepository.findSpaceByParams(name, capacity);
    }

    @Transactional(readOnly = true)
    public List<SpaceResources> getSpaceResources(long spaceId, Set<ResourcesEnum> resources) {
        return spaceResourcesRepository.findSpaceResourceByResourceAndSpaceId(spaceId, resources != null ? resources.stream().toList() : null);
    }
}
