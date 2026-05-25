package com.backend.labpoint.controller;


import com.backend.labpoint.domain.reserves.ReserveRequestDTO;
import com.backend.labpoint.domain.reserves.Reserves;
import com.backend.labpoint.service.ReserveService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.ArraySchema;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.media.Schema.RequiredMode;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.tags.Tag;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.time.LocalDate;
import java.util.List;

@RestController
@RequestMapping("/reserves")
@Tag(name = "/reserves", description = "Endpoints para gerenciamento de reservas")
public class ReserveController {
    @Autowired
    private ReserveService reserveService;

    @Operation(summary = "Busca reservas por espaço e data", description = "Retorna uma lista de reservas para um determinado espaço em uma data específica")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Lista de reservas encontrada", content = @Content(array = @ArraySchema(schema = @Schema(implementation = Reserves.class, requiredMode = RequiredMode.REQUIRED)))),
            @ApiResponse(responseCode = "404", description = "Nenhuma reserva encontrada", content = @Content)
    })
    @GetMapping("/find/{spaceId}/{date}")
    public ResponseEntity<List<Reserves>> getReserve(@PathVariable Long spaceId, @PathVariable LocalDate date) {
        List<Reserves> reserves = reserveService.getReservesByDate(spaceId, date);
        if (reserves.isEmpty()) {
            return ResponseEntity.notFound().build();
        }
        return ResponseEntity.ok(reserves);
    }

    @Operation(summary = "Cria uma nova reserva", description = "Cria uma reserva para um espaço específico na data e horários fornecidos")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "201", description = "Reserva criada com sucesso"),
            @ApiResponse(responseCode = "404", description = "Espaço não encontrado ou erro na criação da reserva")
    })
    @PostMapping("/create/{spaceId}")
    public ResponseEntity<Void> postCreateReserve(@PathVariable Long spaceId, @RequestBody ReserveRequestDTO body) {
        if (!reserveService.createReserve(spaceId, body.date(), body.schedules())) {
            return ResponseEntity.notFound().build();
        }

        return ResponseEntity.status(HttpStatus.CREATED).build();
    }
}
