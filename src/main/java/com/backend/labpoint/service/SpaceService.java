package com.backend.labpoint.service;

import com.backend.labpoint.domain.resource.Resource;
import com.backend.labpoint.domain.space.*;
import com.backend.labpoint.domain.subject.Subject;
import com.backend.labpoint.exception.BadRequestException;
import com.backend.labpoint.exception.ResourceNotFoundException;
import com.backend.labpoint.repository.*;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.domain.Specification;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.Set;

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
    public List<Space> getSpaces(Specification<Space> spec, Pageable pageable) {
        return spaceRepository.findAll(spec, pageable).getContent();
    }

    @Transactional(readOnly = true)
    public long countSpaces(Specification<Space> spec) {
        return spaceRepository.count(spec);
    }

    @Transactional(readOnly = true)
    public List<SpaceResource> getSpaceResourcesBySpaceId(Integer spaceId) {
        return spaceResourceRepository.findSpaceResourceBySpaceId(spaceId);
    }

    @Transactional(readOnly = true)
    public List<SpaceSubject> getSpaceSubjectsBySpaceId(Integer spaceId) {
        return spaceSubjectRepository.findSpaceSubjectBySpaceId(spaceId);
    }

    @Transactional
    public void createSpace(String name, String description, int capacity, Set<Integer> resources, Set<Integer> subjects) {
        boolean hasSpace = spaceRepository.existsByName(name);
        if (hasSpace)
            throw new BadRequestException("Espaço ja existe");

        Space newSpace = new Space(name, description, capacity);
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
    public PatchSpaceResponseDTO updateSpace(Integer id, PatchSpaceRequestDTO dto) {
        if (spaceRepository.existsByName(dto.name()))
            throw new BadRequestException("Nome do espaço ja existe");
        Space space = spaceRepository.findById(id).orElseThrow(() -> new ResourceNotFoundException("Espaço nao encontrado"));

        // Update core fields
        space.setName(dto.name());
        space.setCapacity(dto.capacity());

        // Remove existing associations
        List<SpaceResource> oldResources = spaceResourceRepository.findSpaceResourceBySpaceId(id);
        if (!oldResources.isEmpty())
            spaceResourceRepository.deleteAll(oldResources);
        List<SpaceSubject> oldSubjects = spaceSubjectRepository.findSpaceSubjectBySpaceId(id);
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

        return new PatchSpaceResponseDTO(id, dto.name(), dto.capacity(), dto.resources().stream().toList(), dto.subjects().stream().toList());
    }

    @Transactional
    public void deleteSpaces(Set<Integer> ids) {
        List<Space> spaces = spaceRepository.findByIds(ids.stream().toList());
        if (spaces.isEmpty())
            throw new ResourceNotFoundException("Espaço(s) nao encontrado(s)");
        spaceRepository.deleteAll(spaces);
    }
}
