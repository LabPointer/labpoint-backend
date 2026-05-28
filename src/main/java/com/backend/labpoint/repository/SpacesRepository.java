package com.backend.labpoint.repository;

import com.backend.labpoint.domain.space.Space;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;
import org.springframework.stereotype.Repository;

import java.util.Optional;

@Repository
public interface SpacesRepository extends JpaRepository<Space, Integer>, JpaSpecificationExecutor<Space> {

    Optional<Space> findById(Integer id);

    boolean existsByName(String name);

    /*
    @Query("SELECT s FROM Spaces s " +
            "WHERE (:name IS NULL OR :name = '' OR LOWER(s.name) LIKE LOWER(CONCAT('%', :name, '%'))) " +
            "AND (:capacity IS NULL OR s.capacity >= :capacity)")
    List<Space> findSpaceByParams(@Param("name") String name,
                                   @Param("capacity") Integer capacity);
    */
}

