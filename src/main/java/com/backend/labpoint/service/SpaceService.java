package com.backend.labpoint.service;

import com.backend.labpoint.dto.resource.ResourceDTO;
import com.backend.labpoint.dto.space.PatchSpaceRequestDTO;
import com.backend.labpoint.dto.space.SpaceDTO;
import com.backend.labpoint.dto.space.SpaceRequestDTO;
import com.backend.labpoint.dto.space.SpacesResponseDTO;
import com.backend.labpoint.dto.subject.SubjectDTO;
import com.backend.labpoint.entities.resource.Resource;
import com.backend.labpoint.entities.resource.SpaceResource;
import com.backend.labpoint.entities.space.Space;
import com.backend.labpoint.entities.subject.SpaceSubject;
import com.backend.labpoint.entities.subject.Subject;
import com.backend.labpoint.exception.BadRequestException;
import com.backend.labpoint.exception.ResourceNotFoundException;
import com.backend.labpoint.repository.*;
import com.backend.labpoint.specification.SpaceSpecification;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Pageable;
import org.springframework.data.domain.Sort;
import org.springframework.data.jpa.domain.Specification;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.Set;
import java.util.concurrent.CompletableFuture;

@Service
public class SpaceService {
    @Autowired
    private SpacesRepository spaceRepository;

    @Autowired
    private SpaceResourceRepository spaceResourceRepository;

    @Autowired
    private SpaceSubjectRepository spaceSubjectRepository;

    @Autowired
    private ResourceRepository resourceRepository;

    @Autowired
    private SubjectRepository subjectRepository;

    @Transactional(readOnly = true)
    public ResponseEntity<SpacesResponseDTO> getSpaces(SpaceRequestDTO params) {
        Specification<Space> spec = SpaceSpecification.filters(
                params.name(),
                params.capacity(),
                params.resources(),
                params.subjects(),
                params.locked());

        int offset = params.offset() != null ? params.offset() : 0;
        int limit = params.limit() != null ? params.limit() + 1 : 11;
        Pageable pageable = PageRequest.of(offset / limit, limit, Sort.by("name").ascending());

        List<Space> spaces = spaceRepository.findAll(spec, pageable).getContent();

        if (spaces.isEmpty())
            throw new ResourceNotFoundException("Espaço(s) nao encontrado(s)");

        List<SpaceDTO> spacesResponse = spaces.stream().map(space -> {
            CompletableFuture<List<SpaceResource>> resourcesFuture = CompletableFuture.supplyAsync(space::getResources);

            CompletableFuture<List<SpaceSubject>> subjectsFuture = CompletableFuture.supplyAsync(space::getSubjects);

            CompletableFuture.allOf(resourcesFuture, subjectsFuture).join();

            List<ResourceDTO> resources = resourcesFuture.join().stream().map(sr -> {
                Resource r = sr.getResource();
                return new ResourceDTO(r.getId(), r.getName());
            }).toList();
            List<SubjectDTO> subjects = subjectsFuture.join().stream().map(sr -> {
                Subject s = sr.getSubject();
                return new SubjectDTO(s.getId(), s.getName());
            }).toList();

            return new SpaceDTO(space.getId(), space.getName(), space.getCapacity(), space.getDescription(), resources, subjects, space.isLocked());
        }).toList();

        SpacesResponseDTO response = new SpacesResponseDTO(spacesResponse, params.offset() == null ? 0 : params.offset(), params.limit() == null ? 0 : params.limit());

        return ResponseEntity.ok(response);
    }

    @Transactional(readOnly = true)
    public long countSpaces(Specification<Space> spec) {
        return spaceRepository.count(spec);
    }

    @Transactional(readOnly = true)
    public List<SpaceResource> getSpaceResourcesBySpaceId(Long spaceId) {
        return spaceResourceRepository.findBySpaceId(spaceId);
    }

    @Transactional(readOnly = true)
    public List<SpaceSubject> getSpaceSubjectsBySpaceId(Long spaceId) {
        return spaceSubjectRepository.findBySpaceId(spaceId);
    }

    @Transactional
    public void createSpace(String name, String description, int capacity, Set<Long> resources, Set<Long> subjects) {
        boolean hasSpace = spaceRepository.existsByName(name);
        if (hasSpace)
            throw new BadRequestException("Espaço ja existe");

        Space newSpace = new Space(name, description, capacity, true);
        newSpace = spaceRepository.save(newSpace);

        for (var r : resources) {
            Resource resource = resourceRepository.findById(r).orElseThrow(() -> new ResourceNotFoundException("Recurso nao encontrado"));

            SpaceResource spaceResource = new SpaceResource(newSpace, resource);
            spaceResourceRepository.save(spaceResource);
        }
        for (var s : subjects) {
            Subject subject = subjectRepository.findById(s).orElseThrow(() -> new ResourceNotFoundException("Materia nao encontrada"));

            SpaceSubject spaceSubject = new SpaceSubject(newSpace, subject);
            spaceSubjectRepository.save(spaceSubject);
        }
    }

    @Transactional
    public SpaceResource createSpaceResource(SpaceResource resource) {
        return spaceResourceRepository.save(resource);
    }

    @Transactional
    public SpaceSubject createSpaceSubject(SpaceSubject subject) {
        return spaceSubjectRepository.save(subject);
    }

    @Transactional
    public void updateSpace(Long id, PatchSpaceRequestDTO dto) {
        if (spaceRepository.existsByName(dto.name()))
            throw new BadRequestException("Nome do espaço ja existe");
        Space space = spaceRepository.findById(id).orElseThrow(() -> new ResourceNotFoundException("Espaço nao encontrado"));

        // Update core fields
        space.setName(dto.name());
        space.setCapacity(dto.capacity());

        // Remove existing associations
        List<SpaceResource> oldResources = spaceResourceRepository.findBySpaceId(id);
        if (!oldResources.isEmpty())
            spaceResourceRepository.deleteAll(oldResources);
        List<SpaceSubject> oldSubjects = spaceSubjectRepository.findBySpaceId(id);
        if (!oldSubjects.isEmpty())
            spaceSubjectRepository.deleteAll(oldSubjects);

        // Add new resource links
        List<Resource> resources = resourceRepository.findAllById(dto.resources());
        if (resources.isEmpty())
            throw new ResourceNotFoundException("Recurso(s) nao encontrado(s)");
        List<Subject> subjects = subjectRepository.findAllById(dto.resources());
        if (subjects.isEmpty())
            throw new ResourceNotFoundException("Materia(s) nao encontrada(s)");
        for (Resource r : resources) {
            SpaceResource spaceResource = new SpaceResource();
            spaceResource.setResource(r);
            spaceResource.setSpace(space);
            createSpaceResource(spaceResource);
        }

        // Add new subject links
        for (Subject s : subjects) {
            SpaceSubject spaceSubject = new SpaceSubject();
            spaceSubject.setSubject(s);
            spaceSubject.setSpace(space);
            createSpaceSubject(spaceSubject);
        }

        spaceRepository.save(space);
    }

    @Transactional
    public void deleteSpaces(Set<Long> ids) {
        List<Space> spaces = spaceRepository.findByIdIn(ids.stream().toList());
        if (spaces.isEmpty())
            throw new ResourceNotFoundException("Espaço(s) nao encontrado(s)");
        spaceRepository.deleteAll(spaces);
    }
}
