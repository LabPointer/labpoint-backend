package com.backend.labpoint.repository;

import com.backend.labpoint.entities.reserve.Reserve;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;
import org.springframework.stereotype.Repository;

import java.time.LocalDate;
import java.util.List;

@Repository
public interface ReserveRepository extends JpaRepository<Reserve, Long>, JpaSpecificationExecutor<Reserve> {
    List<Reserve> findBySpace_IdAndReservedDateFromAndReservedDateTo(Long spaceId, LocalDate reservedDateFrom, LocalDate reservedDateTo);
    List<Reserve> findByIdIn(List<Long> ids);
}
