package com.backend.labpoint.repository;

import com.backend.labpoint.domain.resource.Resource;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface ResourceRepository extends JpaRepository<Resource, Integer> {
    @Query("SELECT r FROM Resource r WHERE r.id in :ids")
    List<Resource> findByIds(List<Integer> ids);

    List<Resource> findByName(String name);

    Boolean existsByName(String name);

    @Query("SELECT r FROM Resource r WHERE r.name LIKE %:name%")
    List<Resource> findByNameLike(String name, Pageable pageable);
}
