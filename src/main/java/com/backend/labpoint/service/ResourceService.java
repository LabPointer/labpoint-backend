package com.backend.labpoint.service;

import com.backend.labpoint.dto.resource.ResourceDTO;
import com.backend.labpoint.dto.resource.ResourceRequestDTO;
import com.backend.labpoint.entities.resource.Resource;
import com.backend.labpoint.exception.BadRequestException;
import com.backend.labpoint.exception.ResourceNotFoundException;
import com.backend.labpoint.repository.ResourceRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Pageable;
import org.springframework.data.domain.Sort;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;

import java.util.List;
import java.util.Set;

@Service
public class ResourceService {
    @Autowired
    private ResourceRepository resourceRepository;

    public ResponseEntity<List<ResourceDTO>> getResources(ResourceRequestDTO params) {
        int limit = params.limit() == null ? 11 : params.limit() + 1;
        int offset = params.offset() == null ? 0 : params.offset();
        Pageable pageable = PageRequest.of(offset, limit, Sort.by("name").ascending());

        List<Resource> resources = params.name() == null ? resourceRepository.findAll(pageable).getContent() : resourceRepository.findByNameLike(params.name(), pageable);
        if (resources.isEmpty())
            throw new ResourceNotFoundException("Recurso(s) nao encontrado(s)");

        List<ResourceDTO> resourceDTOs = resources.stream().map(r -> new ResourceDTO(r.getId(), r.getName())).toList();

        return ResponseEntity.ok().body(resourceDTOs);
    }

    public List<ResourceDTO> getResourcesByIds(List<Long> id) {
        return resourceRepository.findByIds(id).stream().map(r -> new ResourceDTO(r.getId(), r.getName())).toList();
    }

    public void createResource(String name) {
        if (resourceRepository.existsByName(name))
            throw new BadRequestException("Recurso já existe");
        Resource newResource = new Resource(name);
        resourceRepository.save(newResource);
    }

    public ResourceDTO updateResource(Long id, String newName) {
        Resource resource = resourceRepository.findById(id).orElseThrow(() -> new ResourceNotFoundException("Recurso nao encontrado"));

        if (resourceRepository.existsByName(newName))
            throw new BadRequestException("Recurso já existe");

        resource.setName(newName);
        resource = resourceRepository.save(resource);

        return new ResourceDTO(resource.getId(), resource.getName());
    }

    public void deleteResources(Set<Long> ids) {
        List<Resource> resources = resourceRepository.findAllById(ids);
        if (resources.isEmpty())
            throw new ResourceNotFoundException("Recurso(s) nao encontrado(s)");
        resourceRepository.deleteAll(resources);
    }
}
