package com.backend.labpoint.service;

import com.backend.labpoint.domain.spaces.SpaceResources;
import com.backend.labpoint.domain.spaces.Spaces;
import com.backend.labpoint.repository.SpaceResourcesRepository;
import com.backend.labpoint.repository.SpacesRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.Set;

@Service
public class SpacesService {
    @Autowired
    private SpacesRepository spaceRepository;

    @Autowired
    private SpaceResourcesRepository spaceResourcesRepository;

    @Transactional(readOnly = true)
    public List<Spaces> getSpaces(String name, Integer capacity) {
        return spaceRepository.findSpaceByParams(name, capacity);
    }

    @Transactional(readOnly = true)
    public List<SpaceResources> getSpaceResourcesByList(long spaceId, Set<String> resources) {
        return spaceResourcesRepository.findSpaceResourceByListResourceAndSpaceId(spaceId, resources != null ? resources.stream().toList() : null);
    }

    @Transactional(readOnly = true)
    public List<SpaceResources> getSpaceResourcesByString(long spaceId, String resource) {
        return spaceResourcesRepository.findSpaceResourceByResourceAndSpaceId(spaceId, resource);
    }

    @Transactional
    public Spaces createSpace(Spaces space) {
        return spaceRepository.save(space);
    }

    @Transactional
    public boolean deleteSpace(long id) {
        /*spaceRepository.findById(id)
            .ifPresent(space -> { return spaceRepository.delete(space); });*/
        var space = spaceRepository.findById(id);
        if (space.isPresent()) {
            spaceRepository.delete(space.get());
            return true;
        }
        return false;
    }

    @Transactional
    public SpaceResources createSpaceResource(SpaceResources resource) {
        return spaceResourcesRepository.save(resource);
    }
}
