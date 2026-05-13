package com.backend.labpoint.controller;

import com.backend.labpoint.domain.error.ErroResponseDTO;
import com.backend.labpoint.domain.spaces.*;
import com.backend.labpoint.service.SpacesService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.ArraySchema;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.media.Schema.RequiredMode;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.ArrayList;
import java.util.List;


@RestController
@RequestMapping("/spaces")
@Tag(name = "/spaces", description = "Endpoints para pesquisa de espaços")
public class SpacesController {
    @Autowired
    private SpacesService spaceService;

    @Operation(summary = "Busca por laboratorios", description = "Retorna uma lista de laboratorios")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Laboratorios encontrados", content = @Content(array = @ArraySchema(schema = @Schema(implementation = SpacesResponseDTO.class, requiredMode = RequiredMode.REQUIRED)))),
            @ApiResponse(responseCode = "404", description = "Nenhum laboratorio encontrado", content = @Content)
    })
    @GetMapping
    public ResponseEntity<List<SpacesResponseDTO>> getSpaces(SpaceRequestDTO params) {
        List<Spaces> spaces = spaceService.getSpaces(params.name(), params.capacity());
        if (spaces == null || spaces.isEmpty()) return ResponseEntity.notFound().build();

        List<SpacesResponseDTO> spacesResponse = new ArrayList<>();
        for (Spaces space : spaces) {
            SpacesResponseDTO spaceResponse = new SpacesResponseDTO();

            if (params.resources() != null && !params.resources().isEmpty()) {
                List<SpaceResources> r = spaceService.getSpaceResourcesByList(space.getId(), params.resources());

                boolean hasResource = true;
                if (params.resources() != null && !params.resources().isEmpty())
                    hasResource = params.resources().stream()
                            .anyMatch(res -> r.stream().anyMatch(sr -> sr.getName().equals(res)));

                if (!hasResource) continue;

                spaceResponse.setResources(r.stream().map(s -> s.getName()).toList());
            } else {
                List<SpaceResources> r = spaceService.getSpaceResourcesBySpaceId(space.getId());
                spaceResponse.setResources(r.stream().map(s -> s.getName()).toList());
            }

            spaceResponse.setSpaceResponse(space);
            spacesResponse.add(spaceResponse);
        }

        return ResponseEntity.ok(spacesResponse);
    }

    @Operation(summary = "Cria um novo espaço", description = "Cria um novo espaço no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "204", description = "Espaço criado com sucesso", content = @Content),
            @ApiResponse(responseCode = "400", description = "Erro ao criar espaço", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PostMapping("/create")
    public ResponseEntity<ErroResponseDTO> postCreateSpace(@RequestBody @Valid CreateSpaceRequestDTO data) {
        Spaces newSpace = new Spaces(data.name(), data.capacity());
        if (spaceService.createSpace(newSpace) == null)
            return ResponseEntity.badRequest()
                    .body(new ErroResponseDTO(
                            HttpStatus.BAD_REQUEST,
                            "Erro ao criar espaço, possivelmente já existe"
                    ));

        for (var resource : data.resources()) {
            SpaceResources newResource = new SpaceResources();
            newResource.setName(resource);
            newResource.setSpace(newSpace);
            spaceService.createSpaceResource(newResource);
        }

        return ResponseEntity.noContent().build();
    }

    @Operation(summary = "Deleta um espaço", description = "Deleta um espaço do sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "204", description = "Espaço deletado com sucesso", content = @Content),
            @ApiResponse(responseCode = "404", description = "Espaço não encontrado", content = @Content)
    })
    @DeleteMapping("/delete/{id}")
    public ResponseEntity<Void> deleteSpace(@PathVariable Long id) {
        if (spaceService.deleteSpace(id))
            return ResponseEntity.noContent().build();

        return ResponseEntity.notFound().build();
    }

}
