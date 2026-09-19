package com.backend.labpoint.repository;

import com.backend.labpoint.entities.resource.SpaceResource;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface SpaceResourceRepository extends JpaRepository<SpaceResource, Long>, JpaSpecificationExecutor<SpaceResource> {
    List<SpaceResource> findBySpaceId(Long spaceId);
}