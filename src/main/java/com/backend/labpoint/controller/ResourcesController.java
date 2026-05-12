package com.backend.labpoint.controller;

import com.backend.labpoint.domain.spaces.SpaceResources;
import com.backend.labpoint.repository.SpaceResourcesRepository;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.ArraySchema;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.tags.Tag;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.CrossOrigin;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;

@RestController
@RequestMapping("/resources")
@Tag(name = "/resources", description = "Endpoints para pesquisa de recursos")
@CrossOrigin
public class ResourcesController {
    @Autowired
    private SpaceResourcesRepository spaceResourcesRepository;

    @Operation(summary = "Busca por recursos", description = "Retorna uma lista de recursos. Destinado ao autocomplete.")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Recursos encontrados", content = @Content(array = @ArraySchema(schema = @Schema(implementation = SpaceResources.class, requiredMode = Schema.RequiredMode.REQUIRED)))),
            @ApiResponse(responseCode = "404", description = "Nenhum recurso encontrado", content = @Content)
    })
    @GetMapping
    public ResponseEntity<List<SpaceResources>> getResources(@RequestParam(required = true) String name) {
        var resources = spaceResourcesRepository.findByName(name);
        if (resources.isEmpty())
            return ResponseEntity.notFound().build();

        return ResponseEntity.ok().body(resources);
    }
}
