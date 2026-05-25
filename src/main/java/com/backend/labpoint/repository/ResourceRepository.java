package com.backend.labpoint.repository;

import java.util.List;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import com.backend.labpoint.domain.resource.Resource;

import jakarta.transaction.Transactional;

@Repository
public interface ResourceRepository extends JpaRepository<Resource, Integer> {
    List<Resource> findByName(String name);

    Boolean existByName(String name);

    @Modifying
    @Transactional
    @Query("UPDATE Resource r SET r.name = :name WHERE r.id = :id")
    void updateNameById(@Param("id") Integer id, @Param("name") String name);
}
