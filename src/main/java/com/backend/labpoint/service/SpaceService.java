package com.backend.labpoint.service;

import com.backend.labpoint.domain.resource.Resource;
import com.backend.labpoint.domain.space.*;
import com.backend.labpoint.domain.subject.Subject;
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
    public boolean createSpace(String name, String description, int capacity, Set<String> resources, Set<String> subjects) {
        boolean hasSpace = spaceRepository.existsByName(name);
        if (hasSpace) return false;

        Space newSpace = new Space(name, description, capacity);
        newSpace = spaceRepository.save(newSpace);

        for (var r : resources) {
            boolean hasResource = resourceRepository.existsByName(r);
            Resource resource = null;
            if (!hasResource) {
                resource = resourceRepository.save(new Resource(r));
            } else {
                resource = resourceRepository.findByName(r).getFirst();
            }

            SpaceResource spaceResource = new SpaceResource(newSpace, resource);
            spaceResourceRepository.save(spaceResource);
        }
        for (var s : subjects) {
            boolean hasSubject = subjectRepository.existsByName(s);
            Subject subject = null;
            if (!hasSubject) {
                subject = subjectRepository.save(new Subject(s));
            } else {
                subject = subjectRepository.findByName(s).getFirst();
            }
            SpaceSubject spaceSubject = new SpaceSubject(newSpace, subject);
            spaceSubjectRepository.save(spaceSubject);
        }

        return true;
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
            throw new IllegalArgumentException("Nome ja existe em outro espaço");
        Space space = spaceRepository.findById(id).orElseThrow(() -> new IllegalArgumentException("Espaço nao encontrado"));

        // Update core fields
        space.setName(dto.name());
        space.setCapacity(dto.capacity());

        // Remove existing associations
        List<SpaceResource> oldResources = spaceResourceRepository.findSpaceResourceBySpaceId(id);
        if (!oldResources.isEmpty()) {
            spaceResourceRepository.deleteAll(oldResources);
        }
        List<SpaceSubject> oldSubjects = spaceSubjectRepository.findSpaceSubjectBySpaceId(id);
        if (!oldSubjects.isEmpty()) {
            spaceSubjectRepository.deleteAll(oldSubjects);
        }

        // Add new resource links
        for (String resourceName : dto.resources()) {
            Resource resource = resourceRepository.findByName(resourceName).stream().findFirst()
                    .orElseGet(() -> resourceRepository.save(new Resource(null, resourceName)));
            SpaceResource spaceResource = new SpaceResource();
            spaceResource.setResource(resource);
            spaceResource.setSpace(space);
            createSpaceResource(spaceResource);
        }

        // Add new subject links
        for (String subjectName : dto.subjects()) {
            Subject subject = subjectRepository.findByName(subjectName).stream().findFirst()
                    .orElseGet(() -> subjectRepository.save(new Subject(null, subjectName)));
            SpaceSubject spaceSubject = new SpaceSubject();
            spaceSubject.setSubject(subject);
            spaceSubject.setSpace(space);
            createSpaceSubject(spaceSubject);
        }

        return new PatchSpaceResponseDTO(id, dto.name(), dto.capacity(), dto.resources().stream().toList(), dto.subjects().stream().toList());
    }

    @Transactional
    public boolean deleteSpace(Integer id) {
        Space space = spaceRepository.findById(id).orElseThrow(() -> new IllegalArgumentException("Espaço nao encontrado"));
        spaceRepository.delete(space);
        return true;
    }
}
