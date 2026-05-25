package com.backend.labpoint.service;

import com.backend.labpoint.domain.reserves.Reserves;
import com.backend.labpoint.domain.reserves.SchedulesEnum;
import com.backend.labpoint.domain.space.Space;
import com.backend.labpoint.repository.ReserveRepository;
import com.backend.labpoint.repository.SpacesRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDate;
import java.util.List;
import java.util.Set;

@Service
public class ReserveService {
    @Autowired
    private SpacesRepository spaceRepository;

    @Autowired
    private ReserveRepository reserveRepository;

    @Transactional(readOnly = true)
    public List<Reserves> getReservesByDate(Long spaceId, LocalDate date) {
        return reserveRepository.findByReservedDateAndSpace_Id(date, spaceId != null ? spaceId.intValue() : null);
    }

    @Transactional
    public boolean createReserve(Long spaceId, LocalDate date, Set<SchedulesEnum> schedules) {
        Space space = spaceRepository.findById(spaceId != null ? spaceId.intValue() : null).stream().findFirst().orElse(null);
        if (space == null) return false;
        List<Reserves> reserves = reserveRepository.findByReservedDateAndSpace_Id(date, spaceId != null ? spaceId.intValue() : null);
        boolean isUnavaliable = false;
        if (reserves != null && !reserves.isEmpty()) {
            isUnavaliable = reserves.stream()
                    .anyMatch(r -> r.getSchedule().equals(schedules));
        }
        if (isUnavaliable) return false;

        schedules.forEach(schedule -> {
            Reserves reserve = new Reserves();
            reserve.setReservedDate(date);
            reserve.setSchedule(schedule);
            reserve.setSpace(space);

            reserveRepository.save(reserve);
        });

        return true;
    }
}
