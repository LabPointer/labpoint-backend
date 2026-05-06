package com.backend.labpoint.repository;

import com.backend.labpoint.entity.ResourcesEnum;
import com.backend.labpoint.entity.SpaceResources;
import com.backend.labpoint.entity.Spaces;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface SpaceResourcesRepository  extends JpaRepository<SpaceResources, Long>, JpaSpecificationExecutor<SpaceResources> {
    /*
    @Query("SELECT sr.space FROM SpaceResources sr " +
            "WHERE sr.space.id = :spaceId " +
            "AND (:resources IS NULL OR sr.resource IN :resources)")
    List<Spaces> findSpace(@Param("spaceId") Long spaceId,
                           @Param("resources") List<ResourcesEnum> resources);
     */

    @Query("SELECT sr FROM SpaceResources sr " +
            "WHERE sr.space.id = :spaceId " +
            "AND (:resources IS NULL OR sr.resource IN :resources)")
    List<SpaceResources> findSpaceResourceByResourceAndSpaceId(@Param("spaceId") Long spaceId,
                                        @Param("resources") List<ResourcesEnum> resources);
}