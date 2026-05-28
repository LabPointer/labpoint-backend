package com.backend.labpoint.repository;

import com.backend.labpoint.domain.space.SpaceSubject;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface SpaceSubjectRepository extends JpaRepository<SpaceSubject, Integer> {

    @Query("SELECT ss FROM SpaceSubject ss WHERE ss.space.id = :spaceId")
    List<SpaceSubject> findSpaceSubjectBySpaceId(@Param("spaceId") Integer spaceId);
}
