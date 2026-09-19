package com.backend.labpoint.service;

import com.backend.labpoint.dto.reserve.*;
import com.backend.labpoint.entities.account.Account;
import com.backend.labpoint.entities.reserve.Reserve;
import com.backend.labpoint.entities.space.Space;
import com.backend.labpoint.entities.reserve.ReserveStatusEnum;
import com.backend.labpoint.entities.schedule.ReserveSchedule;
import com.backend.labpoint.entities.schedule.SchedulesEnum;
import com.backend.labpoint.exception.BadRequestException;
import com.backend.labpoint.exception.ForbiddenException;
import com.backend.labpoint.exception.ResourceNotFoundException;
import com.backend.labpoint.repository.ReserveRepository;
import com.backend.labpoint.repository.ReserveScheduleRepository;
import com.backend.labpoint.repository.SpacesRepository;
import com.backend.labpoint.repository.AccountRepository;
import com.backend.labpoint.specification.ReserveSpecification;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.data.jpa.domain.Specification;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
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
    private AccountRepository userRepository;

    @Autowired
    private SpacesRepository spaceRepository;

    @Autowired
    private ReserveRepository reserveRepository;

    @Autowired
    private ReserveScheduleRepository reserveScheduleRepository;

    // Home reserve
    @Transactional(readOnly = true)
    public ResponseEntity<List<SchedulesEnum>> existingSchedules(Long spaceId, ExistingScheduleRequestDTO params) {
        LocalDate dateFrom = params.dateFrom();
        LocalDate dateTo = params.dateTo();
        Specification<Reserve> reserveSpecification = ReserveSpecification.exists(spaceId, dateFrom, dateTo);
        List<Reserve> existingReserves = reserveRepository.findAll(reserveSpecification);
        if (existingReserves.isEmpty()) {
            throw new ResourceNotFoundException("Nenhuma reserva encontrada para o espaço especificado no intervalo de datas fornecido.");
        }
        List<SchedulesEnum> existingSchedules = existingReserves.stream()
                .flatMap(reserve -> reserve.getSchedules().stream())
                .map(ReserveSchedule::getSchedule)
                .toList();
        if (existingSchedules.isEmpty()) {
            throw new ResourceNotFoundException("Nenhum horário reservado encontrado para o espaço especificado no intervalo de datas fornecido.");
        }

        return ResponseEntity.ok(existingSchedules);
    }

    @Transactional
    public ResponseEntity<?> createReserve(UserDetails userDetails, Long spaceId, CreateReserveRequestDTO createReserveRequestDTO) {
        Account user = userRepository.findByRegistration(userDetails.getUsername())
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
            List<Long> reserveIds = existingReserves.stream().map(Reserve::getId).collect(Collectors.toList());

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
        reserve.setAccount(user);
        reserve.setSpace(space);
        reserve.setReservedDateFrom(createReserveRequestDTO.dateFrom());
        reserve.setReservedDateTo(createReserveRequestDTO.dateTo());
        reserve.setStatus(ReserveStatusEnum.CONFIRMED);
        reserve.setPurpose(createReserveRequestDTO.purpose());

        Reserve newReserve = reserveRepository.save(reserve);

        List<ReserveSchedule> reserveSchedules = createReserveRequestDTO.schedules().stream()
                .map(schedule -> new ReserveSchedule(schedule, newReserve)).toList();

        reserveScheduleRepository.saveAll(reserveSchedules);

        return ResponseEntity.status(HttpStatus.CREATED).build();
    }

    // History
    @Transactional(readOnly = true)
    public ResponseEntity<ReserveDateDTO> getReserveDate(UserDetails userDetails, Long reserveId) {
        Account user = userRepository.findByRegistration(userDetails.getUsername())
                .orElseThrow(() -> new ResourceNotFoundException("User not found"));

        Reserve reserve = reserveRepository.findById(reserveId)
                .orElseThrow(() -> new ResourceNotFoundException("Reserve not found"));

        if (reserve.getAccount().getId() != user.getId())
            throw new ForbiddenException("You are not allowed to access this reserve.", false);

        return ResponseEntity.ok(new ReserveDateDTO(reserve.getReservedDateFrom(), reserve.getReservedDateTo()));
    }

    @Transactional(readOnly = true)
    public ResponseEntity<ReserveHistoryDTO> findHistoryByMonth(UserDetails userDetails, YearMonth yearMonth) {
        Account user = userRepository.findByRegistration(userDetails.getUsername())
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
            if (reserve.getStatus().equals(ReserveStatusEnum.CANCELED)) {
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
    public ResponseEntity<Void> editReserveDate(UserDetails userDetails, Long id, ReserveDateDTO data) {
        Account user = (Account) userRepository.findByRegistration(userDetails.getUsername())
                .orElseThrow(() -> new ResourceNotFoundException("Usuario nao encontrado"));

        Reserve reserve = reserveRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("Reserva nao encontrada"));

        if (!reserve.getAccount().getId().equals(user.getId()))
            throw new ForbiddenException("Você não tem autorização para editar essa reserva", false);

        Space space = reserve.getSpace();

        Specification<Reserve> reserveSpecification = ReserveSpecification.spaceHasReserveByDate(
                space.getId(),
                reserve.getId(),
                data.dateFrom(),
                data.dateTo()
        );

        List<Reserve> previousReserves = reserveRepository.findAll(reserveSpecification);

        if (!previousReserves.isEmpty()) {
            List<SchedulesEnum> currentSchedules = reserve.getSchedules().stream()
                    .map(ReserveSchedule::getSchedule)
                    .toList();

            List<SchedulesEnum> previousSchedules = previousReserves.stream()
                    .flatMap(s -> s.getSchedules().stream())
                    .map(ReserveSchedule::getSchedule)
                    .toList();

            boolean hasScheduleConflict = currentSchedules.stream().anyMatch(previousSchedules::contains);

            if (hasScheduleConflict) {
                throw new BadRequestException("Mudança de data conflita com os horarios ja reservados.");
            }
        }

        reserve.setReservedDateFrom(data.dateFrom());
        reserve.setReservedDateTo(data.dateTo());

        reserveRepository.save(reserve);

        return ResponseEntity.status(HttpStatus.NO_CONTENT).build();
    }

    @Transactional
    public ResponseEntity<Void> cancelReserveFromHistory(UserDetails userDetails, Long id) {
        Account user = (Account) userRepository.findByRegistration(userDetails.getUsername())
                .orElseThrow(() -> new ResourceNotFoundException("User not found"));
        Reserve reserve = reserveRepository.findById(id)
                .orElseThrow(() -> new ResourceNotFoundException("Reserve not found"));
        if (reserve.getAccount().getId() != user.getId())
            throw new ForbiddenException("You are not authorized to cancel this reserve", false);

        reserve.setStatus(ReserveStatusEnum.CANCELED);

        reserveRepository.save(reserve);

        return ResponseEntity.status(HttpStatus.NO_CONTENT).build();
    }
}
