package com.backend.labpoint.controller;


import com.backend.labpoint.domain.error.ErroResponseDTO;
import com.backend.labpoint.domain.reserve.*;
import com.backend.labpoint.domain.user.User;
import com.backend.labpoint.service.ReserveService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.ArraySchema;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.media.Schema.RequiredMode;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.constraints.NotEmpty;
import org.springdoc.core.annotations.ParameterObject;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.data.repository.query.Param;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.web.bind.annotation.*;

import java.time.LocalDate;
import java.util.Objects;
import java.util.Set;

@RestController
@RequestMapping("/reserves")
@Tag(name = "/reserves", description = "Endpoints para gerenciamento de reservas")
public class ReserveController {
    @Autowired
    private ReserveService reserveService;

    @Operation(summary = "Buscar reservas por espaço e data", description = "Retorna uma lista de reservas para um determinado espaço em uma data específica")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Lista de reservas encontrada", content = @Content(array = @ArraySchema(schema = @Schema(implementation = ReserveResponseDTO.class, requiredMode = RequiredMode.REQUIRED)))),
            @ApiResponse(responseCode = "404", description = "Nenhuma reserva encontrada", content = @Content)
    })
    @GetMapping
    public ResponseEntity<Object> getReserves(@ParameterObject ReserveRequestDTO params) {
        return reserveService.findReserves(params.yearMonth(), params.spaceName(), params.username(), params.registration());
    }

    @Operation(summary = "Buscar reservas por espaço e data", description = "Retorna uma lista de reservas para um determinado espaço em uma data específica")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Lista de reservas encontrada", content = @Content(array = @ArraySchema(schema = @Schema(implementation = ReserveResponseDTO.class, requiredMode = RequiredMode.REQUIRED)))),
            @ApiResponse(responseCode = "404", description = "Nenhuma reserva encontrada", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED)))
    })
    @GetMapping("/find/{spaceId}")
    public ResponseEntity<Object> getReservesFromSpace(@PathVariable Integer spaceId, @Param("dates") @NotEmpty Set<LocalDate> dates) {
        return reserveService.findReservesBySpace(spaceId, dates);
    }

    @Operation(summary = "Criar uma nova reserva", description = "Criar uma reserva para um espaço específico na data e horários fornecidos")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "201", description = "Reserva criada com sucesso", content = @Content),
            @ApiResponse(responseCode = "404", description = "Espaço não encontrado ou erro na criação da reserva", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED))),
            @ApiResponse(responseCode = "403", description = "Usuario nao é administrador", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED)))
    })
    @PostMapping("/create/{spaceId}")
    public ResponseEntity<Object> postCreateReserve(@AuthenticationPrincipal UserDetails userDetails, @PathVariable Integer spaceId, @RequestBody CreateReserveRequestDTO data) {
        String registration = userDetails.getUsername();
        if (data.lock() != null && userDetails.getAuthorities().stream().anyMatch(a -> Objects.equals(a.getAuthority(), "ROLE_ADMIN"))) {
            return ResponseEntity.status(HttpStatus.FORBIDDEN).body(new ErroResponseDTO("Usuario precisa ser admin para bloquear reservas"));
        }
        User user = reserveService.findUserByRegistration(registration);
        return reserveService.createReserve(user, spaceId, data);
    }

    @Operation(summary = "Atualizar as informações da reserva", description = "Atualiza as informações da reserva")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Reserva(s) deletadas com sucesso", content = @Content(schema = @Schema(implementation = Reserve.class, requiredMode = RequiredMode.REQUIRED))),
            @ApiResponse(responseCode = "404", description = "Reserva(s) nao encontrada", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED))),
            @ApiResponse(responseCode = "403", description = "Usuario nao é administrador e tentou alterar a reserva de outro usuario", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED)))
    })
    @PatchMapping("/update/{reserveId}")
    public ResponseEntity<Object> updateReserve(@AuthenticationPrincipal UserDetails userDetails, @PathVariable Integer reserveId, @RequestBody UpdateReserveRequestDTO data) {
        String registration = userDetails.getUsername();
        User user = reserveService.findUserByRegistration(registration);
        return reserveService.updateReserve(user, reserveId, data);
    }

    @Operation(summary = "Deletar um conjunto de reservas", description = "Deleta/cancela um conjunto de reservas do sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "204", description = "Reserva(s) deletadas com sucesso", content = @Content),
            @ApiResponse(responseCode = "404", description = "Reserva(s) nao encontrada", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED)))
    })
    @DeleteMapping("/delete")
    public ResponseEntity<Object> deleteReserve(@AuthenticationPrincipal UserDetails userDetails, @RequestBody DeleteReserveRequestDTO data) {
        String registration = userDetails.getUsername();
        User user = reserveService.findUserByRegistration(registration);
        return reserveService.deleteReserve(user, data.reserveIds());
    }
}
