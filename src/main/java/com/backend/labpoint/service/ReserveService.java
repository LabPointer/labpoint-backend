package com.backend.labpoint.service;

import com.backend.labpoint.domain.reserves.Reserve;
import com.backend.labpoint.domain.reserves.SchedulesEnum;
import com.backend.labpoint.domain.space.Space;
import com.backend.labpoint.domain.user.User;
import com.backend.labpoint.repository.ReserveRepository;
import com.backend.labpoint.repository.SpacesRepository;
import com.backend.labpoint.repository.UserRepository;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.security.Principal;
import java.time.LocalDate;
import java.util.List;
import java.util.Set;

@Service
public class ReserveService {

    @Autowired
    private UserRepository userRepository;

    @Autowired
    private SpacesRepository spaceRepository;

    @Autowired
    private ReserveRepository reserveRepository;

    public User findUserByRegistration(String registration) {
        return userRepository.findByRegistration(registration).stream().findFirst().orElse(null);
    }

    public Space findSpaceById(Long id) {
        return spaceRepository.findById(id != null ? id.intValue() : null).stream().findFirst().orElse(null);
    }

    @Transactional(readOnly = true)
    public List<Reserve> getReservesByDate(Long spaceId, LocalDate date) {
        return reserveRepository.findByReservedDateAndSpace_Id(date, spaceId != null ? spaceId.intValue() : null);
    }

    @Transactional
    public boolean createReserve(User user, Long spaceId, LocalDate date, Set<SchedulesEnum> schedules) {
        Space space = spaceRepository.findById(spaceId != null ? spaceId.intValue() : null).stream().findFirst().orElse(null);
        if (space == null) return false;
        List<Reserve> reserves = reserveRepository.findByReservedDateAndSpace_Id(date, spaceId != null ? spaceId.intValue() : null);
        boolean isUnavaliable = false;
        if (reserves != null && !reserves.isEmpty()) {
            isUnavaliable = reserves.stream()
                    .anyMatch(r -> r.getSchedule().equals(schedules));
        }
        if (isUnavaliable) return false;

        schedules.forEach(schedule -> {
            Reserve reserve = new Reserve(date, schedule, user, space);

            reserveRepository.save(reserve);
        });

        return true;
    }
}
