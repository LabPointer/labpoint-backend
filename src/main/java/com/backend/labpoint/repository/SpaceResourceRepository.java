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
    /*
    @Query("SELECT sr.space FROM SpaceResources sr " +
            "WHERE sr.space.id = :spaceId " +
            "AND (:resources IS NULL OR sr.resource IN :resources)")
    List<Spaces> findSpace(@Param("spaceId") Long spaceId,
                           @Param("resources") List<ResourcesEnum> resources);
     */

    List<SpaceResource> findByName(String name);
    
    Boolean existByName(String name);

    @Query("SELECT sr FROM SpaceResources sr " +
            "WHERE sr.space.id = :spaceId ")
    List<SpaceResource> findSpaceResourceBySpaceId(@Param("spaceId") Integer spaceId);

    @Query("SELECT sr FROM SpaceResources sr " +
            "WHERE sr.space.id = :spaceId " +
            "AND (:resources IS NULL OR sr.name IN :resources)")
    List<SpaceResource> findSpaceResourceByListResourceAndSpaceId(@Param("spaceId") Integer spaceId,
            @Param("resources") List<String> resources);

    @Query("SELECT sr FROM SpaceResources sr " +
            "WHERE sr.space.id = :spaceId " +
            "AND (:resource IS NULL OR sr.name LIKE CONCAT('%', :resource, '%'))")
    List<SpaceResource> findSpaceResourceByResourceAndSpaceId(@Param("spaceId") Integer spaceId,
                                                               @Param("resource") String resource);
}