package com.backend.labpoint.service;

import com.backend.labpoint.domain.error.ErroResponseDTO;
import com.backend.labpoint.domain.reserve.*;
import com.backend.labpoint.domain.space.Space;
import com.backend.labpoint.domain.user.User;
import com.backend.labpoint.repository.ReserveRepository;
import com.backend.labpoint.repository.SpacesRepository;
import com.backend.labpoint.repository.UserRepository;
import com.backend.labpoint.specification.ReserveSpecification;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.data.domain.Sort;
import org.springframework.data.jpa.domain.Specification;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDate;
import java.time.YearMonth;
import java.util.*;

@Service
public class ReserveService {

    @Autowired
    private UserRepository userRepository;

    @Autowired
    private SpacesRepository spaceRepository;

    @Autowired
    private ReserveRepository reserveRepository;

    @Transactional(readOnly = true)
    public User findUserByRegistration(String registration) {
        return userRepository.findByRegistration(registration).stream().findFirst().orElse(null);
    }

    @Transactional(readOnly = true)
    public ResponseEntity<Object> findReservesBySpace(Integer spaceId, Set<LocalDate> dates) {
        LocalDate now = LocalDate.now();

        Set<LocalDate> orderedDates = new TreeSet<>(dates);

        LocalDate beforeDate = null;
        ArrayList<ReserveResponseDTO> reservesResponse = new ArrayList<>();
        for (LocalDate currentDate : orderedDates) {
            if (currentDate.isBefore(now)) {
                return ResponseEntity.badRequest().body("Ordem de datas invalidas");
            }

            if (beforeDate != null && currentDate.isBefore(beforeDate)) {
                return ResponseEntity.badRequest().body(new ErroResponseDTO("As datas não podem ser anteriores ao dia de hoje"));
            }

            beforeDate = currentDate;

            List<Reserve> reserves = reserveRepository.findBySpace_IdAndReservedDate(spaceId, currentDate);
            ReserveResponseDTO reserveRes = new ReserveResponseDTO(currentDate, reserves);
            reservesResponse.add(reserveRes);
        }

        if (reservesResponse.isEmpty()) return ResponseEntity.notFound().build();

        return ResponseEntity.ok(reservesResponse);
    }

    @Transactional(readOnly = true)
    public ResponseEntity<Object> findReserves(YearMonth yearMonth, String spaceName, String username, String registration) {
        Specification<Reserve> spec = ReserveSpecification.filters(yearMonth, spaceName, username, registration);

        List<Reserve> reserves = reserveRepository.findAll(spec, Sort.by(Sort.Direction.DESC, "reservedDate"));
        if (reserves.isEmpty()) return ResponseEntity.notFound().build();

        return ResponseEntity.ok(reserves);
    }

    public Space findSpaceById(Long id) {
        return spaceRepository.findById(id != null ? id.intValue() : null).stream().findFirst().orElse(null);
    }

    @Transactional(readOnly = true)
    public List<Reserve> getReservesByDate(Long spaceId, LocalDate date) {
        return reserveRepository.findBySpace_IdAndReservedDate(spaceId != null ? spaceId.intValue() : null, date);
    }

    @Transactional
    public ResponseEntity<Object> createReserve(User user, Integer spaceId, CreateReserveRequestDTO data) {
        Specification<Reserve> spec = ReserveSpecification.exists(spaceId, data.dates(), data.schedules());

        boolean reserveAlreadyExists = reserveRepository.exists(spec);

        if (reserveAlreadyExists) {
            return ResponseEntity.badRequest().body("Já existe reserva para este espaço em uma das datas e horários informados");
        }

        Space space = spaceRepository.findById(spaceId).orElseThrow();

        Set<LocalDate> orderedDates = new TreeSet<>(data.dates());
        LocalDate now = LocalDate.now();

        LocalDate beforeDate = null;
        for (LocalDate currentDate : orderedDates) {
            if (currentDate.isBefore(now)) {
                return ResponseEntity.badRequest().body("Ordem de datas invalidas");
            }

            if (beforeDate != null && currentDate.isBefore(beforeDate)) {
                return ResponseEntity.badRequest().body("As datas não podem ser anteriores ao dia de hoje");
            }

            beforeDate = currentDate;

            for (SchedulesEnum schedule : data.schedules()) {
                Reserve reserve = new Reserve(currentDate, schedule, user, space);
                reserveRepository.save(reserve);
            }

        }

        return ResponseEntity.status(HttpStatus.CREATED).build();
    }

    @Transactional
    public ResponseEntity<Object> updateReserve(User user, Integer id, UpdateReserveRequestDTO data) {
        boolean isAdmin = user.getAuthorities().stream().anyMatch(a -> Objects.equals(a.getAuthority(), "ROLE_ADMIN"));
        Optional<Reserve> optReserve = reserveRepository.findById(id);
        if (optReserve.isEmpty()) return ResponseEntity.notFound().build();
        Reserve reserve = optReserve.get();
        if (isAdmin) {
            if (data.reservedDate() != null)
                reserve.setReservedDate(data.reservedDate());
            if (data.schedule() != null)
                reserve.setSchedule(data.schedule());
            if (data.userRegistration() != null)
                reserve.setUser(userRepository.findByRegistration(data.userRegistration()).orElseThrow());
            if (data.spaceId() != null)
                reserve.setSpace(spaceRepository.findById(data.spaceId()).orElseThrow());
            reserve = reserveRepository.save(reserve);
            return ResponseEntity.ok(reserve);
        } else {
            if (reserve.getUser().getId() != user.getId())
                return ResponseEntity.status(HttpStatus.FORBIDDEN).build();

            if (data.userRegistration() != null)
                return ResponseEntity.status(HttpStatus.FORBIDDEN).build();

            if (data.reservedDate() != null)
                reserve.setReservedDate(data.reservedDate());

            if (data.schedule() != null)
                reserve.setSchedule(data.schedule());

            if (data.spaceId() != null)
                reserve.setSpace(spaceRepository.findById(data.spaceId()).orElseThrow());

            reserve = reserveRepository.save(reserve);
            return ResponseEntity.ok(reserve);
        }
    }

    @Transactional
    public ResponseEntity<Object> deleteReserve(User user, Set<Integer> ids) {
        boolean isAdmin = user.getAuthorities().stream().anyMatch(a -> Objects.equals(a.getAuthority(), "ROLE_ADMIN"));
        List<Reserve> reserves = reserveRepository.findByIds(ids.stream().toList());
        if (reserves.isEmpty()) return ResponseEntity.notFound().build();
        if (!isAdmin) {
            reserves.removeIf(r -> r.getUser().getId() != user.getId());
        }
        reserveRepository.deleteAll(reserves);

        return ResponseEntity.status(HttpStatus.NO_CONTENT).build();
    }
}
