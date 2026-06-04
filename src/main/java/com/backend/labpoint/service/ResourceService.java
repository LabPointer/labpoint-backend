package com.backend.labpoint.service;

import com.backend.labpoint.domain.resource.Resource;
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
    public List<Resource> getResources() {
        return resourceRepository.findAll();
    }

    @CacheEvict(value = "resources", allEntries = true)
    public void createResource(String name) {
        if (resourceRepository.existsByName(name))
            throw new BadRequestException("Recurso já existe");
        Resource newResource = new Resource(null, name);
        resourceRepository.save(newResource);
    }

    @CachePut("resources")
    public Resource updateResource(Integer id, String newName) {
        Resource resource = resourceRepository.findById(id).orElseThrow(() -> new ResourceNotFoundException("Recurso nao encontrado"));

        if (resourceRepository.existsByName(newName))
            throw new BadRequestException("Recurso já existe");

        resource.setName(newName);
        resource = resourceRepository.save(resource);

        return resource;
    }

    @CacheEvict(value = "resources", allEntries = true)
    public void deleteResources(Set<Integer> ids) {
        List<Resource> resources = resourceRepository.findAllById(ids);
        if (resources.isEmpty())
            throw new ResourceNotFoundException("Recurso(s) nao encontrado(s)");
        resourceRepository.deleteAll(resources);
    }
}
