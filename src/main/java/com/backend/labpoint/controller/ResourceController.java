package com.backend.labpoint.controller;

import com.backend.labpoint.domain.error.ErroResponseDTO;
import com.backend.labpoint.domain.resource.Resource;
import com.backend.labpoint.repository.ResourceRepository;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.ArraySchema;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.parameters.RequestBody;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.constraints.NotBlank;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Pageable;
import org.springframework.data.domain.Sort;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/resources")
@Tag(name = "/resources", description = "Endpoints para pesquisa de recursos")
public class ResourceController {
    @Autowired
    private ResourceRepository resourceRepository;

    @Operation(summary = "Busca por recursos", description = "Retorna uma lista de recursos. Destinado ao autocomplete.")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Recursos encontrados", content = @Content(array = @ArraySchema(schema = @Schema(implementation = Resource.class, requiredMode = Schema.RequiredMode.REQUIRED)))),
            @ApiResponse(responseCode = "404", description = "Nenhum recurso encontrado", content = @Content)
    })
    @GetMapping
    public ResponseEntity<List<Resource>> getResources(@RequestParam(required = false) String name, @RequestParam(required = false) Integer page, @RequestParam(required = false) Integer size) {
        int limit = size == null ? 10 : size;
        if (limit > 50) limit = 50;
        if (limit < 10) limit = 10;
        int offset = page == null ? 0 : (page - 1) * limit;
        if (offset < 0) offset = 0;
        Pageable pageable = PageRequest.of(offset, limit, Sort.by("name").ascending());
        var resources = name == null ? resourceRepository.findAll() : resourceRepository.findByNameLike(name, pageable);
        if (resources.isEmpty())
            return ResponseEntity.notFound().build();

        return ResponseEntity.ok().body(resources);
    }

    @Operation(summary = "Cria um recurso", description = "Cria um recurso no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "201", description = "Recurso criado com sucesso", content = @Content),
            @ApiResponse(responseCode = "400", description = "Erro ao criar recurso", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PostMapping("/create")
    public ResponseEntity<Object> postCreateResource(@RequestBody @NotBlank String name) {
        if (resourceRepository.existsByName(name))
            return ResponseEntity.badRequest().body(new ErroResponseDTO("Recurso já existe"));
        Resource newResource = new Resource(null, name);
        resourceRepository.save(newResource);
        return ResponseEntity.status(HttpStatus.CREATED).build();
    }

    @Operation(summary = "Edita um recurso", description = "Edita um recurso no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Recurso editado com sucesso", content = @Content(schema = @Schema(implementation = Resource.class, requiredMode = Schema.RequiredMode.REQUIRED))),
            @ApiResponse(responseCode = "404", description = "Recurso não encontrado", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class))),
            @ApiResponse(responseCode = "400", description = "Erro ao editar recurso", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PatchMapping("/update/{id}")
    public ResponseEntity<Object> updateResource(@PathVariable Integer id, @RequestBody String newName) {
        if (!resourceRepository.existsById(id))
            return ResponseEntity.notFound().build();

        var resource = resourceRepository.findById(id).orElseThrow();

        if (resourceRepository.existsByName(newName))
            return ResponseEntity.badRequest().body(new ErroResponseDTO("Recurso já existe"));

        resource.setName(newName);
        resource = resourceRepository.save(resource);

        return ResponseEntity.ok(resource);
    }

    @Operation(summary = "Deleta um recurso", description = "Deleta um recurso no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "204", description = "Recurso deletado com sucesso", content = @Content),
            @ApiResponse(responseCode = "404", description = "Recurso não encontrado", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @DeleteMapping("/delete/{id}")
    public ResponseEntity<Object> deleteResource(@PathVariable Integer id) {
        if (!resourceRepository.existsById(id))
            return ResponseEntity.notFound().build();

        resourceRepository.deleteById(id);
        return ResponseEntity.noContent().build();
    }
}
