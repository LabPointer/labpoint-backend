package com.backend.labpoint.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import com.backend.labpoint.domain.space.SpaceResource;

import java.util.List;

@Repository
public interface SpaceResourceRepository extends JpaRepository<SpaceResource, Integer>, JpaSpecificationExecutor<SpaceResource> {
    @Query("SELECT sr FROM SpaceResource sr WHERE sr.space.id = :spaceId")
    List<SpaceResource> findSpaceResourceBySpaceId(@Param("spaceId") Integer spaceId);
}