package com.backend.labpoint.repository;

import com.backend.labpoint.entities.schedule.ReserveSchedule;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;

import java.util.List;

public interface ReserveScheduleRepository  extends JpaRepository<ReserveSchedule, Long>, JpaSpecificationExecutor<ReserveSchedule> {
    List<ReserveSchedule> findByReserveId(Long id);

    List<ReserveSchedule> findByReserveIdIn(List<Long> ids);
}
