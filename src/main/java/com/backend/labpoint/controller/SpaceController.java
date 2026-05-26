package com.backend.labpoint.controller;

import com.backend.labpoint.domain.error.ErroResponseDTO;
import com.backend.labpoint.domain.space.*;
import com.backend.labpoint.domain.subject.Subject;
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
import org.springframework.data.domain.Sort;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.ArrayList;
import java.util.List;

import com.backend.labpoint.repository.ResourceRepository;
import com.backend.labpoint.repository.SubjectRepository;
import com.backend.labpoint.domain.resource.Resource;
import com.backend.labpoint.specification.SpaceSpecification;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.domain.Specification;

@RestController
@RequestMapping("/spaces")
@Tag(name = "/spaces", description = "Endpoints para pesquisa de espaços")
public class SpaceController {
    @Autowired
    private SpaceService spaceService;

    @Autowired
    private ResourceRepository resourceRepository;

    @Autowired
    private SubjectRepository subjectRepository;

    @Operation(summary = "Busca por laboratorios", description = "Retorna uma lista de laboratorios")
    @ApiResponses(value = {
            // @ApiResponse(responseCode = "200", description = "Laboratorios encontrados",
            // content = @Content(array = @ArraySchema(schema = @Schema(implementation =
            // SpaceDTO.class, requiredMode = RequiredMode.REQUIRED)))),
            @ApiResponse(responseCode = "200", description = "Laboratorios encontrados", content = @Content(schema = @Schema(implementation = SpacesResponseDTO.class, requiredMode = RequiredMode.REQUIRED))),
            @ApiResponse(responseCode = "404", description = "Nenhum laboratorio encontrado", content = @Content)
    })
    @GetMapping
    public ResponseEntity<SpacesResponseDTO> getSpaces(@ParameterObject @ModelAttribute SpaceRequestDTO params) {
        Specification<Space> spec = SpaceSpecification.filters(
                params.name(),
                params.capacity(),
                params.resources(),
                params.subjects());

        int total = (int) spaceService.countSpaces(spec);

        int offset = params.offset() != null ? params.offset() : 0;
        int limit = params.limit() != null ? params.limit() : 10;
        Pageable pageable = PageRequest.of(offset / limit, limit, Sort.by("name").ascending());

        List<Space> spaces = spaceService.getSpaces(spec, pageable);
        if (spaces == null || spaces.isEmpty())
            return ResponseEntity.notFound().build();

        List<SpaceDTO> spacesResponse = new ArrayList<>();
        for (Space space : spaces) {
            List<SpaceResource> spaceResources = spaceService.getSpaceResourcesBySpaceId(space.getId());
            List<SpaceSubject> spaceSubjects = spaceService.getSpaceSubjectsBySpaceId(space.getId());

            SpaceDTO spaceResponse = new SpaceDTO();
            spaceResponse.setId(space.getId());
            spaceResponse.setName(space.getName());
            spaceResponse.setCapacity(space.getCapacity());
            spaceResponse.setResources(spaceResources.stream()
                    .map(sr -> sr.getResource().getName()).toList());
            spaceResponse.setSubjects(spaceSubjects.stream()
                    .map(ss -> ss.getSubject().getName()).toList());

            spacesResponse.add(spaceResponse);
        }

        SpacesResponseDTO response = new SpacesResponseDTO();
        response.setSpaces(spacesResponse);
        response.setOffset(params.offset());
        response.setLimit(params.limit());
        response.setTotal(total);

        return ResponseEntity.ok(response);
    }

    @Operation(summary = "Cria um novo espaço", description = "Cria um novo espaço no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "201", description = "Espaço criado com sucesso", content = @Content),
            @ApiResponse(responseCode = "400", description = "Erro ao criar espaço", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PostMapping("/create")
    public ResponseEntity<ErroResponseDTO> postCreateSpace(@RequestBody @Valid CreateSpaceRequestDTO data) {
        var success = spaceService.createSpace(data.name(), data.description(), data.capacity(), data.resources(), data.subjects());
        if (!success) {
            return ResponseEntity.badRequest()
                    .body(new ErroResponseDTO("Erro ao criar espaço, possivelmente já existe"));
        }

        return ResponseEntity.status(HttpStatus.CREATED).build();
    }

    @Operation(summary = "Editar espaço", description = "Edita um espaço existente")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Espaço editado com sucesso", content = @Content),
            @ApiResponse(responseCode = "400", description = "Erro ao editar espaço", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PatchMapping("/update/{id}")
    public ResponseEntity<Object> patchSpace(@PathVariable Integer id, @RequestBody @Valid PatchSpaceRequestDTO data) {
        try {
            var updatedSpace = spaceService.updateSpace(id, data);
            return ResponseEntity.ok(updatedSpace);
        } catch (IllegalArgumentException e) {
            return ResponseEntity.badRequest()
                    .body(new ErroResponseDTO(e.getMessage()));
        }
    }

    @Operation(summary = "Deleta um espaço", description = "Deleta um espaço do sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "204", description = "Espaço deletado com sucesso", content = @Content),
            @ApiResponse(responseCode = "404", description = "Espaço não encontrado", content = @Content)
    })
    @DeleteMapping("/delete/{id}")
    public ResponseEntity<Void> deleteSpace(@PathVariable Integer id) {
        if (spaceService.deleteSpace(id))
            return ResponseEntity.notFound().build();

        return ResponseEntity.noContent().build();
    }
}
