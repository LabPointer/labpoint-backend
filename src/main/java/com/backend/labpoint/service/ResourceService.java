package com.backend.labpoint.service;

import com.backend.labpoint.dto.resource.ResourceDTO;
import com.backend.labpoint.entities.resource.Resource;
import com.backend.labpoint.exception.BadRequestException;
import com.backend.labpoint.exception.ResourceNotFoundException;
import com.backend.labpoint.repository.ResourceRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.cache.annotation.CacheEvict;
import org.springframework.cache.annotation.CachePut;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.stereotype.Service;

import java.util.List;
import java.util.Set;

@Service
public class ResourceService {
    @Autowired
    private ResourceRepository resourceRepository;

    @Cacheable("resources")
    public List<ResourceDTO> getResources() {
        return resourceRepository.findAll().stream().map(r -> new ResourceDTO(r.getId(), r.getName())).toList();
    }

    public List<ResourceDTO> getResourcesByIds(List<Integer> id) {
        return resourceRepository.findByIds(id).stream().map(r -> new ResourceDTO(r.getId(), r.getName())).toList();
    }

    @CacheEvict(value = "resources", allEntries = true)
    public void createResource(String name) {
        if (resourceRepository.existsByName(name))
            throw new BadRequestException("Recurso já existe");
        Resource newResource = new Resource(name);
        resourceRepository.save(newResource);
    }

    @CachePut("resources")
    public ResourceDTO updateResource(Integer id, String newName) {
        Resource resource = resourceRepository.findById(id).orElseThrow(() -> new ResourceNotFoundException("Recurso nao encontrado"));

        if (resourceRepository.existsByName(newName))
            throw new BadRequestException("Recurso já existe");

        resource.setName(newName);
        resource = resourceRepository.save(resource);

        return new ResourceDTO(resource.getId(), resource.getName());
    }

    @CacheEvict(value = "resources", allEntries = true)
    public void deleteResources(Set<Integer> ids) {
        List<Resource> resources = resourceRepository.findAllById(ids);
        if (resources.isEmpty())
            throw new ResourceNotFoundException("Recurso(s) nao encontrado(s)");
        resourceRepository.deleteAll(resources);
    }
}
