package com.backend.labpoint.repository;

import org.springframework.data.jpa.repository.JpaRepository;

import com.backend.labpoint.domain.subject.Subject;

import java.util.List;

public interface SubjectRepository extends JpaRepository<Subject, Integer> {
    List<Subject> findByName(String name);
    List<Subject> findByNameContaining(String nome);
    Boolean existsByName(String name);
}
