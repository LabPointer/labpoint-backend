package com.backend.labpoint.controller;

import com.backend.labpoint.domain.error.ErroResponseDTO;
import com.backend.labpoint.domain.reserve.*;
import com.backend.labpoint.service.ReserveService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.ArraySchema;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.media.Schema.RequiredMode;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.tags.Tag;
import org.springdoc.core.annotations.ParameterObject;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.format.annotation.DateTimeFormat;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.web.bind.annotation.*;

import java.time.LocalDate;
import java.time.YearMonth;
import java.util.List;

@RestController
@RequestMapping("/reserve")
@Tag(name = "/reserve", description = "Endpoints para gerenciamento de reservas")
public class ReserveController {
    @Autowired
    private ReserveService reserveService;

    @Operation(summary = "Buscar historico de reservas do mes", description = "Retorna uma lista de reservas(confirmada, concluida e cancelada) de um mes especifico")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Lista de reservas encontrada", content = @Content(schema = @Schema(implementation = ReserveHistoryDTO.class, requiredMode = RequiredMode.REQUIRED))),
            @ApiResponse(responseCode = "404", description = "Nenhuma reserva encontrada", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED)))
    })
    @GetMapping
    public ResponseEntity<ReserveHistoryDTO> getHistory(@AuthenticationPrincipal UserDetails userDetails, @RequestParam(name = "yearMonth") @DateTimeFormat(pattern = "yyyy-M") YearMonth yearMonth) {
        return reserveService.findHistoryByMonth(userDetails, yearMonth);
    }

    @Operation(summary = "Cria uma nova reserva", description = "Cria uma nova reserva para o espaço especificado, com base nas datas fornecidas e no usuário autenticado")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "201", description = "Reserva criada com sucesso", content = @Content(array = @ArraySchema(schema = @Schema(implementation = SchedulesEnum.class)))),
            @ApiResponse(responseCode = "400", description = "Dados da reserva inválidos ou conflitantes", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED))),
            @ApiResponse(responseCode = "404", description = "Autenticação do usuario ou espaço não encontrada", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED)))
    })
    @GetMapping("/existing-schedules/{spaceId}")
    public ResponseEntity<List<SchedulesEnum>> getExistingSchedules(@PathVariable Integer spaceId, @ParameterObject ExistingScheduleRequestDTO params) {
        return reserveService.existingSchedules(spaceId, params);
    }

    @Operation(summary = "Obtem a data da reserva pelo id", description = "Retorna a data da reserva")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Data da reserve", content = @Content(schema = @Schema(implementation = ReserveDateDTO.class, requiredMode = RequiredMode.REQUIRED))),
            @ApiResponse(responseCode = "404", description = "Reserva nao encontrada", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED)))
    })
    @GetMapping("/date-info/{reserveId}")
    public ResponseEntity<ReserveDateDTO> getDateInfo(@AuthenticationPrincipal UserDetails userDetails, @PathVariable Integer reserveId) {
        return reserveService.getReserveDate(userDetails, reserveId);
    }

    @Operation(summary = "Edita a data da reserva", description = "Edita a data da reserva")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "204", description = "Reserva editada com sucesso", content = @Content),
            @ApiResponse(responseCode = "400", description = "Reserva conflita com horarios ja reservados", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED))),
            @ApiResponse(responseCode = "403", description = "Nao pode editar reserva de outro usuario", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED))),
            @ApiResponse(responseCode = "404", description = "Reserva nao encontrada", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED)))
    })
    @PatchMapping("/edit-date/{reserveId}")
    public ResponseEntity<Void> editReserve(@AuthenticationPrincipal UserDetails userDetails, @PathVariable Integer reserveId, @RequestBody ReserveDateDTO data) {
        return reserveService.editReserveDate(userDetails, reserveId, data);
    }

    @Operation(summary = "Cria uma nova reserva", description = "Cria uma nova reserva para o espaço especificado, com base nas datas fornecidas e no usuário autenticado")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "201", description = "Reserva criada com sucesso", content = @Content),
            @ApiResponse(responseCode = "400", description = "Dados da reserva inválidos ou conflitantes", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED))),
            @ApiResponse(responseCode = "404", description = "Autenticação do usuario ou espaço não encontrada", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED)))
    })
    @PostMapping("/create/{spaceId}")
    public ResponseEntity<?> createReserve(@AuthenticationPrincipal UserDetails userDetails, @PathVariable Integer spaceId, @RequestBody CreateReserveRequestDTO body) {
        return reserveService.createReserve(userDetails, spaceId, body);
    }

    @Operation(summary = "Cancela uma reserva do historico do usuario pelo id", description = "Marca a reserva como cancelada")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "204", description = "Reserva cancelada", content = @Content),
            @ApiResponse(responseCode = "403", description = "Provavelmente esta alterando reserva de outro usuario", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED))),
            @ApiResponse(responseCode = "404", description = "Usuario ou reserva encontrada", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED)))
    })
    @DeleteMapping("/history/cancel/{id}")
    public ResponseEntity<Void> getHistoryByYearMonth(@AuthenticationPrincipal UserDetails userDetails, @PathVariable Integer id) {
        return reserveService.cancelReserveFromHistory(userDetails, id);
    }
}
