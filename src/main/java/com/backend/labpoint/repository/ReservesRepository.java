package com.backend.labpoint.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import com.backend.labpoint.entity.Reserves;

import java.time.LocalDate;
import java.util.List;

@Repository
public interface ReservesRepository extends JpaRepository<Reserves, Long> {
    List<Reserves> findByReservedDateAndSpace_Id(LocalDate reservedDate, Long spaceId);
/*
    @Query("SELECT r FROM Reserve r " +
            "WHERE r.reservedDate = :date " +
            "AND r.space.id = :spaceId")
    List<Reserve> findReserveByDate(@Param("spaceId") Long spaceId,
                                    @Param("date") LocalDate date);
*/
}
