package com.backend.labpoint.repository;

import com.backend.labpoint.entities.space.Space;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;
import org.springframework.data.jpa.repository.Query;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

@Repository
public interface SpacesRepository extends JpaRepository<Space, Long>, JpaSpecificationExecutor<Space> {
    List<Space> findByIdIn(List<Long> ids);
    Optional<Space> findById(Long id);
    boolean existsByName(String name);
}

