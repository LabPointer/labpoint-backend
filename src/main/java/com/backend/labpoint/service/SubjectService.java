package com.backend.labpoint.service;

import com.backend.labpoint.dto.subject.SubjectDTO;
import com.backend.labpoint.entities.subject.Subject;
import com.backend.labpoint.exception.BadRequestException;
import com.backend.labpoint.exception.ResourceNotFoundException;
import com.backend.labpoint.repository.SubjectRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.cache.annotation.CacheEvict;
import org.springframework.cache.annotation.CachePut;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.stereotype.Service;

import java.util.List;
import java.util.Set;

@Service
public class SubjectService {
    @Autowired
    private SubjectRepository subjectRepository;

    @Cacheable("subjects")
    public List<SubjectDTO> getSubjects() {
        return subjectRepository.findAll().stream().map(s -> new SubjectDTO(s.getId(), s.getName())).toList();
    }

    public List<SubjectDTO> getSubjectsByIds(List<Integer> id) {
        return subjectRepository.findByIds(id).stream().map(s -> new SubjectDTO(s.getId(), s.getName())).toList();
    }

    @CacheEvict(value = "subjects", allEntries = true)
    public void createSubject(String name) {
        if (subjectRepository.existsByName(name))
            throw new BadRequestException("Recurso já existe");
        Subject newResource = new Subject(null, name);
        subjectRepository.save(newResource);
    }

    @CachePut("subjects")
    public SubjectDTO updateSubject(Integer id, String newName) {
        Subject subject = subjectRepository.findById(id).orElseThrow(() -> new ResourceNotFoundException("Recurso nao encontrado"));

        if (subjectRepository.existsByName(newName))
            throw new BadRequestException("Recurso já existe");

        subject.setName(newName);
        subject = subjectRepository.save(subject);

        return new SubjectDTO(subject.getId(), subject.getName());
    }

    @CacheEvict(value = "subjects", allEntries = true)
    public void deleteSubjects(Set<Integer> ids) {
        List<Subject> subjects = subjectRepository.findAllById(ids);
        if (subjects.isEmpty())
            throw new ResourceNotFoundException("Recurso(s) nao encontrado(s)");
        subjectRepository.deleteAll(subjects);
    }
}
