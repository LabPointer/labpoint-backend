package com.backend.labpoint.repository;

import com.backend.labpoint.entities.resource.Resource;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface ResourceRepository extends JpaRepository<Resource, Long> {
    List<Resource> findByIdIn(List<Long> ids);
    List<Resource> findByName(String name);
    Boolean existsByName(String name);
    List<Resource> findByNameContaining(String name, Pageable pageable);
}
