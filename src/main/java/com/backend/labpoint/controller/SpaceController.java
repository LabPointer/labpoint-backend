package com.backend.labpoint.controller;

import com.backend.labpoint.dto.error.ErroResponseDTO;
import com.backend.labpoint.dto.space.*;
import com.backend.labpoint.service.SpaceService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.media.Schema.RequiredMode;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import org.springdoc.core.annotations.ParameterObject;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/spaces")
@Tag(name = "/spaces", description = "Endpoints para pesquisa de espaços")
public class SpaceController {
    @Autowired
    private SpaceService spaceService;

    @Operation(summary = "Buscar por laboratorios", description = "Retorna uma lista de laboratorios")
    @ApiResponses(value = {
            // @ApiResponse(responseCode = "200", description = "Laboratorios encontrados",
            // content = @Content(array = @ArraySchema(schema = @Schema(implementation =
            // SpaceDTO.class, requiredMode = RequiredMode.REQUIRED)))),
            @ApiResponse(responseCode = "200", description = "Laboratorios encontrados", content = @Content(schema = @Schema(implementation = SpacesResponseDTO.class, requiredMode = RequiredMode.REQUIRED))),
            @ApiResponse(responseCode = "404", description = "Nenhum laboratorio encontrado", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED)))
    })
    @GetMapping
    public ResponseEntity<SpacesResponseDTO> getSpaces(@ParameterObject @ModelAttribute SpaceRequestDTO params) {
        return spaceService.getSpaces(params);
    }

    @Operation(summary = "Criar um novo espaço", description = "Cria um novo espaço no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "201", description = "Espaço criado com sucesso", content = @Content),
            @ApiResponse(responseCode = "400", description = "Erro ao criar espaço", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PostMapping("/create")
    public ResponseEntity<Void> postCreateSpace(@RequestBody @Valid CreateSpaceRequestDTO data) {
        spaceService.createSpace(data.name(), data.description(), data.capacity(), data.resources(), data.subjects());

        return ResponseEntity.status(HttpStatus.CREATED).build();
    }

    @Operation(summary = "Editar espaço", description = "Edita um espaço existente")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Espaço editado com sucesso", content = @Content),
            @ApiResponse(responseCode = "400", description = "Erro ao editar espaço", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PatchMapping("/update/{id}")
    public ResponseEntity<Void> patchSpace(@PathVariable Long id, @RequestBody @Valid PatchSpaceRequestDTO data) {
        spaceService.updateSpace(id, data);
        return ResponseEntity.created(null).build();
    }

    @Operation(summary = "Deletar um espaço", description = "Deleta um espaço do sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "204", description = "Espaço deletado com sucesso", content = @Content),
            @ApiResponse(responseCode = "404", description = "Espaço não encontrado", content = @Content)
    })
    @DeleteMapping("/delete")
    public ResponseEntity<Void> deleteSpace(@RequestBody DeleteSpaceDTO data) {
        spaceService.deleteSpaces(data.spaceIds());

        return ResponseEntity.noContent().build();
    }
}
