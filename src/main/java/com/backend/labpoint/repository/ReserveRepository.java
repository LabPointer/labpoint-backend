package com.backend.labpoint.repository;

import com.backend.labpoint.domain.reserve.Reserve;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;
import org.springframework.data.jpa.repository.Query;
import org.springframework.stereotype.Repository;

import java.time.LocalDate;
import java.util.List;

@Repository
public interface ReserveRepository extends JpaRepository<Reserve, Integer>, JpaSpecificationExecutor<Reserve> {
    List<Reserve> findBySpace_IdAndReservedDate(Integer spaceId, LocalDate reservedDate);

    @Query("SELECT r FROM Reserve r WHERE r.id in :ids")
    List<Reserve> findByIds(List<Integer> ids);
/*
    @Query("SELECT r FROM Reserve r " +
            "WHERE r.reservedDate = :dates " +
            "AND r.space.id = :spaceId")
    List<Reserve> findReserveByDate(@Param("spaceId") Long spaceId,
                                    @Param("dates") LocalDate dates);
*/
}
