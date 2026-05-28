package com.backend.labpoint.repository;

import com.backend.labpoint.domain.subject.Subject;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface SubjectRepository extends JpaRepository<Subject, Integer> {
    List<Subject> findByName(String name);

    Boolean existsByName(String name);

    @Query("SELECT s FROM Subject s WHERE s.name LIKE %:name%")
    List<Subject> findByNameContaining(String name, Pageable pageable);
}
