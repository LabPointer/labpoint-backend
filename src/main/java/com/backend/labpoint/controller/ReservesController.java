package com.backend.labpoint.controller;


import com.backend.labpoint.dto.body.ReserveBody;
import com.backend.labpoint.entity.Reserves;
import com.backend.labpoint.service.ReservesService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.time.LocalDate;
import java.util.List;

@RestController
@RequestMapping("/reserves")
public class ReservesController {
    @Autowired
    private ReservesService reserveService;

    @GetMapping("/find/{spaceId}/{date}")
    public ResponseEntity<List<Reserves>> getReserve(@PathVariable long spaceId, @PathVariable LocalDate date) {
        List<Reserves> spaces = reserveService.getReservesByDate(spaceId, date);
        return ResponseEntity.ok(spaces);
    }

    @PostMapping("/create/:spaceId")
    public ResponseEntity postCreateReserve(@PathVariable long spaceId, @RequestBody ReserveBody body) {
        if (!reserveService.createReserve(spaceId, body.date(), body.schedules())) {
            return ResponseEntity.notFound().build();
        }

        return ResponseEntity.noContent().build();
    }
}
