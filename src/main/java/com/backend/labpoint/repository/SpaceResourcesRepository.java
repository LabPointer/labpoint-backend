package com.backend.labpoint.repository;

import com.backend.labpoint.domain.spaces.SpaceResources;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface SpaceResourcesRepository extends JpaRepository<SpaceResources, Integer>, JpaSpecificationExecutor<SpaceResources> {
    /*
    @Query("SELECT sr.space FROM SpaceResources sr " +
            "WHERE sr.space.id = :spaceId " +
            "AND (:resources IS NULL OR sr.resource IN :resources)")
    List<Spaces> findSpace(@Param("spaceId") Long spaceId,
                           @Param("resources") List<ResourcesEnum> resources);
     */

    List<SpaceResources> findByName(String name);

    @Query("SELECT sr FROM SpaceResources sr " +
            "WHERE sr.space.id = :spaceId " +
            "AND (:resources IS NULL OR sr.name IN :resources)")
    List<SpaceResources> findSpaceResourceByListResourceAndSpaceId(@Param("spaceId") Long spaceId,
            @Param("resources") List<String> resources);

    @Query("SELECT sr FROM SpaceResources sr " +
            "WHERE sr.space.id = :spaceId " +
            "AND (:resource IS NULL OR sr.name LIKE CONCAT('%', :resource, '%'))")
    List<SpaceResources> findSpaceResourceByResourceAndSpaceId(@Param("spaceId") Long spaceId,
                                                               @Param("resource") String resource);
}