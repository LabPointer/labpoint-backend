package com.backend.labpoint.controller;


import com.backend.labpoint.dto.body.ReservRequestDTO;
import com.backend.labpoint.domain.reserves.Reserves;
import com.backend.labpoint.service.ReservesService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.tags.Tag;

import java.time.LocalDate;
import java.util.List;

@RestController
@RequestMapping("/reserves")
@Tag(name = "/reserves", description = "Endpoints para gerenciamento de reservas")
public class ReservesController {
    @Autowired
    private ReservesService reserveService;

    @Operation(summary = "Busca reservas por espaço e data", description = "Retorna uma lista de reservas para um determinado espaço em uma data específica")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Lista de reservas encontrada"),
            @ApiResponse(responseCode = "404", description = "Nenhuma reserva encontrada")
    })
    @GetMapping("/find/{spaceId}/{date}")
    public ResponseEntity<List<Reserves>> getReserve(@PathVariable long spaceId, @PathVariable LocalDate date) {
        List<Reserves> spaces = reserveService.getReservesByDate(spaceId, date);
        return ResponseEntity.ok(spaces);
    }

    @Operation(summary = "Cria uma nova reserva", description = "Cria uma reserva para um espaço específico na data e horários fornecidos")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "204", description = "Reserva criada com sucesso"),
            @ApiResponse(responseCode = "404", description = "Espaço não encontrado ou erro na criação da reserva")
    })
    @PostMapping("/create/{spaceId}")
    public ResponseEntity<Void> postCreateReserve(@PathVariable long spaceId, @RequestBody ReservRequestDTO body) {
        if (reserveService.createReserve(spaceId, body.date(), body.schedules())) {
            return ResponseEntity.notFound().build();
        }

        return ResponseEntity.noContent().build();
    }
}
