package com.backend.labpoint.repository;

import com.backend.labpoint.domain.reserves.Reserve;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.time.LocalDate;
import java.util.List;

@Repository
public interface ReserveRepository extends JpaRepository<Reserve, Integer> {
    List<Reserve> findByReservedDateAndSpace_Id(LocalDate reservedDate, Integer spaceId);
/*
    @Query("SELECT r FROM Reserve r " +
            "WHERE r.reservedDate = :date " +
            "AND r.space.id = :spaceId")
    List<Reserve> findReserveByDate(@Param("spaceId") Long spaceId,
                                    @Param("date") LocalDate date);
*/
}
