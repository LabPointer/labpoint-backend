package com.backend.labpoint.service;

import com.backend.labpoint.domain.reserves.Reserves;
import com.backend.labpoint.domain.reserves.SchedulesEnum;
import com.backend.labpoint.domain.spaces.Spaces;
import com.backend.labpoint.repository.ReservesRepository;
import com.backend.labpoint.repository.SpacesRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDate;
import java.util.List;
import java.util.Set;

@Service
public class ReservesService {
    @Autowired
    private SpacesRepository spaceRepository;

    @Autowired
    private ReservesRepository reserveRepository;

    @Transactional(readOnly = true)
    public List<Reserves> getReservesByDate(long spaceId, LocalDate date) {
        return  reserveRepository.findByReservedDateAndSpace_Id(date, spaceId);
    }

    @Transactional(readOnly = true)
    public boolean createReserve(long spaceId, LocalDate date, Set<SchedulesEnum> schedules) {
        Spaces space = spaceRepository.findById(spaceId).stream().findFirst().orElse(null);
        if (space == null) return false;
        List<Reserves> reserves = reserveRepository.findByReservedDateAndSpace_Id(date, spaceId);
        boolean isAvailable = false;
        if (reserves != null || !reserves.isEmpty()) {
            isAvailable = reserves.stream()
                    .noneMatch(r -> r.getSchedule().equals(schedules));
        }
        if (isAvailable) return false;

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
