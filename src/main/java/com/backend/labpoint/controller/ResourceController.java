package com.backend.labpoint.controller;

import com.backend.labpoint.dto.error.ErroResponseDTO;
import com.backend.labpoint.dto.resource.DeleteResourceRequestDTO;
import com.backend.labpoint.dto.resource.ResourceCreateRequestDTO;
import com.backend.labpoint.dto.resource.ResourceDTO;
import com.backend.labpoint.dto.resource.ResourceRequestDTO;
import com.backend.labpoint.dto.resource.ResourceUpdateRequestDTO;
import com.backend.labpoint.entities.resource.Resource;
import com.backend.labpoint.service.ResourceService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.ArraySchema;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.parameters.RequestBody;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.tags.Tag;

import org.springdoc.core.annotations.ParameterObject;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/resources")
@Tag(name = "/resources", description = "Endpoints para pesquisa de recursos")
public class ResourceController {
    @Autowired
    private ResourceService resourceService;

    @Operation(summary = "Buscar por recursos", description = "Retorna uma lista de recursos.")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Recursos encontrados", content = @Content(array = @ArraySchema(schema = @Schema(implementation = Resource.class, requiredMode = Schema.RequiredMode.REQUIRED)))),
            @ApiResponse(responseCode = "404", description = "Nenhum recurso encontrado", content = @Content)
    })
    @GetMapping
    public ResponseEntity<List<ResourceDTO>> getResources(@ParameterObject ResourceRequestDTO params) {
        return resourceService.getResources(params);
    }

    @Operation(summary = "Criar um recurso", description = "Cria um recurso no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "201", description = "Recurso criado com sucesso", content = @Content),
            @ApiResponse(responseCode = "400", description = "Erro ao criar recurso", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PostMapping("/manage/create")
    public ResponseEntity<Object> postCreateResource(@RequestBody ResourceCreateRequestDTO data) {
        resourceService.createResource(data.name());
        return ResponseEntity.status(HttpStatus.CREATED).build();
    }

    @Operation(summary = "Editar um recurso", description = "Edita um recurso no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Recurso editado com sucesso", content = @Content(schema = @Schema(implementation = Resource.class, requiredMode = Schema.RequiredMode.REQUIRED))),
            @ApiResponse(responseCode = "404", description = "Recurso não encontrado", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class))),
            @ApiResponse(responseCode = "400", description = "Erro ao editar recurso", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PatchMapping("/manage/update/{id}")
    public ResponseEntity<ResourceDTO> updateResource(@PathVariable Long id, @RequestBody ResourceUpdateRequestDTO data) {
        return ResponseEntity.ok(resourceService.updateResource(id, data.name()));
    }

    @Operation(summary = "Deletar um recurso", description = "Deleta um recurso no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "204", description = "Recurso deletado com sucesso", content = @Content),
            @ApiResponse(responseCode = "404", description = "Recurso não encontrado", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @DeleteMapping("/manage/delete")
    public ResponseEntity<Object> deleteResource(@RequestBody DeleteResourceRequestDTO data) {
        resourceService.deleteResources(data.resourceIds());
        return ResponseEntity.noContent().build();
    }
}
