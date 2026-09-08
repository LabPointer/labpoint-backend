package com.backend.labpoint.service;

import com.backend.labpoint.domain.reserve.*;
import com.backend.labpoint.domain.space.Space;
import com.backend.labpoint.domain.user.User;
import com.backend.labpoint.exception.BadRequestException;
import com.backend.labpoint.exception.ForbiddenException;
import com.backend.labpoint.exception.ResourceNotFoundException;
import com.backend.labpoint.repository.ReserveRepository;
import com.backend.labpoint.repository.ReserveScheduleRepository;
import com.backend.labpoint.repository.SpacesRepository;
import com.backend.labpoint.repository.UserRepository;
import com.backend.labpoint.specification.ReserveSpecification;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.data.crossstore.ChangeSetPersister;
import org.springframework.data.domain.Sort;
import org.springframework.data.jpa.domain.Specification;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.scheduling.annotation.Schedules;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDate;
import java.time.YearMonth;
import java.util.*;
import java.util.stream.Collectors;

@Service
public class ReserveService {

    @Autowired
    private UserRepository userRepository;

    @Autowired
    private SpacesRepository spaceRepository;

    @Autowired
    private ReserveRepository reserveRepository;

    @Autowired
    private ReserveScheduleRepository reserveScheduleRepository;

    // Home reserve
    public ResponseEntity<List<SchedulesEnum>> existingSchedules(Integer spaceId, ExistingScheduleRequestDTO params) {
        LocalDate dateFrom = params.dateFrom();
        LocalDate dateTo = params.dateTo();
        Specification<Reserve> reserveSpecification = ReserveSpecification.exists(spaceId, dateFrom, dateTo);
        List<Reserve> existingReserves = reserveRepository.findAll(reserveSpecification);
        List<Integer> reserveIds = existingReserves.stream().map(Reserve::getId).toList();

        List<SchedulesEnum> existingSchedules = reserveScheduleRepository.findByReserveIdIn(reserveIds).stream()
                .map(ReserveSchedule::getSchedule).toList();

        return ResponseEntity.ok(existingSchedules);
    }

    public ResponseEntity<?> createReserve(UserDetails userDetails, Integer spaceId,
            CreateReserveRequestDTO createReserveRequestDTO) {
        User user = userRepository.findByRegistration(userDetails.getUsername())
                .orElseThrow(() -> new ResourceNotFoundException("User not found"));
        Space space = spaceRepository.findById(spaceId)
                .orElseThrow(() -> new ResourceNotFoundException("Space not found"));

        if (createReserveRequestDTO.dateFrom().isAfter(createReserveRequestDTO.dateTo())) {
            throw new BadRequestException("Data de inicio nao pode ser maior que a data final");
        }

        // Check if the reserve already exists
        Specification<Reserve> reserveSpecification = ReserveSpecification.exists(spaceId,
                createReserveRequestDTO.dateFrom(), createReserveRequestDTO.dateTo());
        List<Reserve> existingReserves = reserveRepository.findAll(reserveSpecification);
        if (!existingReserves.isEmpty()) {
            List<Integer> reserveIds = existingReserves.stream().map(Reserve::getId).collect(Collectors.toList());

            Specification<ReserveSchedule> reserveScheduleSpecification = ReserveSpecification
                    .scheduleExists(reserveIds, createReserveRequestDTO.schedules());
            List<ReserveSchedule> existingSchedules = reserveScheduleRepository.findAll(reserveScheduleSpecification);
            List<SchedulesEnum> existingSchedulesEnum = existingSchedules.stream().map(ReserveSchedule::getSchedule)
                    .toList();
            if (existingSchedules != null
                    && !new HashSet<>(existingSchedulesEnum).containsAll(createReserveRequestDTO.schedules().stream().toList())) {
                throw new BadRequestException("Ja existe uma reserva para este espaco com os horarios selecionados");
            }
        }

        Reserve reserve = new Reserve();
        reserve.setUser(user);
        reserve.setSpace(space);
        reserve.setReservedDateFrom(createReserveRequestDTO.dateFrom());
        reserve.setReservedDateTo(createReserveRequestDTO.dateTo());
        reserve.setStatus(ScheduleStatusEnum.CONFIRMED);
        reserve.setPurpose(createReserveRequestDTO.purpose());

        Reserve newReserve = reserveRepository.save(reserve);

        List<ReserveSchedule> reserveSchedules = createReserveRequestDTO.schedules().stream()
                .map(schedule -> new ReserveSchedule(schedule, newReserve)).toList();

        reserveScheduleRepository.saveAll(reserveSchedules);

        return ResponseEntity.status(HttpStatus.CREATED).build();
    }

    // History

    public ResponseEntity<ReserveHistoryDTO> findHistoryByMonth(UserDetails userDetails, YearMonth yearMonth) {
        User user = userRepository.findByRegistration(userDetails.getUsername())
                .orElseThrow(() -> new ResourceNotFoundException("User not found"));

        Specification<Reserve> reserveHistory = ReserveSpecification.history(yearMonth, user.getId());
        List<Reserve> reserves = reserveRepository.findAll(reserveHistory);

        if (reserves.isEmpty())
            throw new ResourceNotFoundException("Nenhuma reserva foi encontrada.");

        List<ReserveScheduleDTO> next = new ArrayList<>();
        List<ReserveScheduleDTO> concluded = new ArrayList<>();
        List<ReserveScheduleDTO> canceled = new ArrayList<>();

        reserves.forEach(reserve -> {
            List<ReserveSchedule> schedules = reserve.getSchedules();
            if (schedules == null || schedules.isEmpty())
                return;

            LocalDate hoje = LocalDate.now();
            ReserveSummaryDTO reserveSummary = new ReserveSummaryDTO(reserve.getId(), reserve.getSpace().getName(), reserve.getSpace().getCapacity(), reserve.getReservedDateFrom(), reserve.getReservedDateTo(), reserve.getStatus(), reserve.getPurpose());
            List<SchedulesEnum> schedulesEnum = schedules.stream().map(ReserveSchedule::getSchedule).toList();
            if (reserve.getStatus().equals(ScheduleStatusEnum.CANCELED)) {
                canceled.add(new ReserveScheduleDTO(reserveSummary, schedulesEnum));
            } else if (reserve.getReservedDateTo().isBefore(hoje)) {
                concluded.add(new ReserveScheduleDTO(reserveSummary, schedulesEnum));
            } else {
                next.add(new ReserveScheduleDTO(reserveSummary, schedulesEnum));
            }
        });

        return ResponseEntity.ok(new ReserveHistoryDTO(next, concluded, canceled));
    }

    @Transactional
    public ResponseEntity<Void> cancelReserveFromHistory(UserDetails userDetails, Integer id) {
        User user = (User) userRepository.findByRegistration(userDetails.getUsername())
                .orElseThrow(() -> new ResourceNotFoundException("User not found"));
        Reserve reserve = reserveRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("Reserve not found"));
        if (reserve.getUser().getId() != user.getId())
            throw new ForbiddenException("You are not authorized to cancel this reserve");

        reserve.setStatus(ScheduleStatusEnum.CANCELED);

        reserveRepository.save(reserve);

        return ResponseEntity.status(HttpStatus.NO_CONTENT).build();
    }

    @Transactional
    public ResponseEntity<?> deleteReserve(User user, Set<Integer> ids) {
        boolean isAdmin = user.getAuthorities().stream().anyMatch(a -> Objects.equals(a.getAuthority(), "ROLE_ADMIN"));
        List<Reserve> reserves = reserveRepository.findAllById(ids);
        if (reserves.isEmpty())
            throw new ResourceNotFoundException("Reserva(s) nao encontrada(s)");
        if (!isAdmin)
            reserves.removeIf(r -> r.getUser().getId() != user.getId());

        reserveRepository.deleteAll(reserves);

        return ResponseEntity.status(HttpStatus.NO_CONTENT).build();
    }
}
