package com.backend.labpoint.controller;

import com.backend.labpoint.dto.response.SpacesResponse;
import com.backend.labpoint.entity.ResourcesEnum;
import com.backend.labpoint.entity.SpaceResources;
import com.backend.labpoint.entity.Spaces;
import com.backend.labpoint.service.SpacesService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.tags.Tag;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.util.ArrayList;
import java.util.List;
import java.util.Set;

@RestController
@RequestMapping("/spaces")
@Tag(name = "/spaces", description = "Endpoints para pesquisa de espaços")
public class SpacesController {
    @Autowired
    private SpacesService spaceService;

    @Operation(summary = "Busca por laboratorios", description = "Retorna uma lista de laboratorios")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Laboratorios encontrados"),
            @ApiResponse(responseCode = "404", description = "Nenhum laboratorio encontrado")
    })
    @GetMapping
    public ResponseEntity<List<SpacesResponse>> getSpaces(@RequestParam(name = "nome", required = false) String name,
                                                          @RequestParam(name = "capacity", required = false) Integer capacity,
                                                          @RequestParam(name = "resources", required = false) Set<ResourcesEnum> resources) {
        List<Spaces> spaces = spaceService.getSpaces(name, capacity);
        if (spaces == null || spaces.isEmpty()) return ResponseEntity.notFound().build();

        List<SpacesResponse> spacesResponse = new ArrayList<>();
        for (Spaces space : spaces) {
            SpacesResponse spaceResponse = new SpacesResponse();

            if (resources != null && !resources.isEmpty()) {
                List<SpaceResources> r = spaceService.getSpaceResources(space.getId(), space.getResources());

                boolean hasResource = true;
                if (resources != null && !resources.isEmpty())
                    hasResource = resources.stream()
                            .anyMatch(resEnum -> r.stream().anyMatch(sr -> sr.getResource().equals(resEnum)));

                if (!hasResource) continue;

                spaceResponse.setResources(r);
            }

            spaceResponse.setSpace(space);
            spacesResponse.add(spaceResponse);
        }

        return ResponseEntity.ok(spacesResponse);
    }
}
