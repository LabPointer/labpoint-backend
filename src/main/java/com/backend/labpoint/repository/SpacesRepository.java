package com.backend.labpoint.repository;

import java.util.List;
import java.util.Optional;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import com.backend.labpoint.entity.Spaces;

@Repository
public interface SpacesRepository extends JpaRepository<Spaces, Long>, JpaSpecificationExecutor<Spaces> {

    Optional<Spaces> findById(long id);

    @Query("SELECT s FROM Spaces s " +
            "WHERE (:name IS NULL OR :name = '' OR LOWER(s.name) LIKE LOWER(CONCAT('%', :name, '%'))) " +
            "AND (:capacity IS NULL OR s.capacity >= :capacity)")
    List<Spaces> findSpaceByParams(@Param("name") String name,
                                   @Param("capacity") Integer capacity);
}

