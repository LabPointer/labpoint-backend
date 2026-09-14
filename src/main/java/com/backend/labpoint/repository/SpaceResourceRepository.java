package com.backend.labpoint.repository;

import com.backend.labpoint.entities.resource.SpaceResource;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface SpaceResourceRepository extends JpaRepository<SpaceResource, Long>, JpaSpecificationExecutor<SpaceResource> {
    @Query("SELECT sr FROM SpaceResource sr WHERE sr.space.id = :spaceId")
    List<SpaceResource> findSpaceResourceBySpaceId(@Param("spaceId") Long spaceId);
}