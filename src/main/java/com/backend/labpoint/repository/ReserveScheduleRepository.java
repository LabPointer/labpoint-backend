package com.backend.labpoint.repository;

import com.backend.labpoint.entities.schedule.ReserveSchedule;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;
import org.springframework.data.jpa.repository.Query;

import java.util.List;

public interface ReserveScheduleRepository  extends JpaRepository<ReserveSchedule, Integer>, JpaSpecificationExecutor<ReserveSchedule> {
    @Query("SELECT rs FROM ReserveSchedule rs WHERE rs.reserve.id = :id")
    List<ReserveSchedule> findByReserveId(Integer id);

    @Query("SELECT rs FROM ReserveSchedule rs WHERE rs.reserve.id IN :ids")
    List<ReserveSchedule> findByReserveIdIn(List<Integer> ids);
}
