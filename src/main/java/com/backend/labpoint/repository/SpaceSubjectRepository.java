package com.backend.labpoint.repository;

import com.backend.labpoint.entities.subject.SpaceSubject;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface SpaceSubjectRepository extends JpaRepository<SpaceSubject, Long> {
    List<SpaceSubject> findBySpaceId(Long spaceId);
}
