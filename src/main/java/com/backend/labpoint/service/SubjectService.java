package com.backend.labpoint.service;

import com.backend.labpoint.dto.subject.SubjectDTO;
import com.backend.labpoint.dto.subject.SubjectRequestDTO;
import com.backend.labpoint.entities.subject.Subject;
import com.backend.labpoint.exception.BadRequestException;
import com.backend.labpoint.exception.ResourceNotFoundException;
import com.backend.labpoint.repository.SubjectRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.data.domain.Pageable;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Sort;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;

import java.util.List;
import java.util.Set;

@Service
public class SubjectService {
    @Autowired
    private SubjectRepository subjectRepository;

    public ResponseEntity<List<SubjectDTO>> getSubjects(SubjectRequestDTO params) {
        String name = params.name();
        Integer limit = params.limit() == null ? 11 : params.limit() + 1;
        Integer offset = params.offset() == null ? 0 : params.offset();

        Pageable pageable = PageRequest.of(offset, limit, Sort.by("name").ascending());
        List<Subject> subjects = name == null ? subjectRepository.findAll() : subjectRepository.findByNameContaining(name, pageable);

        if (subjects.isEmpty())
            throw new ResourceNotFoundException("Materia(s) nao encontrada(s)");

        List<SubjectDTO> subjectDTOs = subjects.stream().map(s -> new SubjectDTO(s.getId(), s.getName())).toList();

        return ResponseEntity.ok(subjectDTOs);
    }

    public List<SubjectDTO> getSubjectsByIds(List<Long> id) {
        return subjectRepository.findByIds(id).stream().map(s -> new SubjectDTO(s.getId(), s.getName())).toList();
    }

    public void createSubject(String name) {
        if (subjectRepository.existsByName(name))
            throw new BadRequestException("Recurso já existe");
        Subject newResource = new Subject(null, name);
        subjectRepository.save(newResource);
    }

    public SubjectDTO updateSubject(Long id, String newName) {
        Subject subject = subjectRepository.findById(id).orElseThrow(() -> new ResourceNotFoundException("Recurso nao encontrado"));

        if (subjectRepository.existsByName(newName))
            throw new BadRequestException("Recurso já existe");

        subject.setName(newName);
        subject = subjectRepository.save(subject);

        return new SubjectDTO(subject.getId(), subject.getName());
    }

    public void deleteSubjects(Set<Long> ids) {
        List<Subject> subjects = subjectRepository.findAllById(ids);
        if (subjects.isEmpty())
            throw new ResourceNotFoundException("Recurso(s) nao encontrado(s)");
        subjectRepository.deleteAll(subjects);
    }
}
