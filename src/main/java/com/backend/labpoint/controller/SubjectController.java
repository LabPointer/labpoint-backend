package com.backend.labpoint.controller;

import com.backend.labpoint.domain.error.ErroResponseDTO;
import com.backend.labpoint.domain.subject.Subject;
import com.backend.labpoint.repository.SubjectRepository;
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
@RequestMapping("/subjects")
@Tag(name = "/subjects", description = "Endpoints para pesquisa de disciplinas")
public class SubjectController {

    @Autowired
    private SubjectRepository subjectRepository;

    @Operation(summary = "Listar todas as matérias", description = "Lista todas as matérias cadastradas")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Matérias listadas com sucesso",
                    content = @Content(array = @ArraySchema(schema = @Schema(implementation = Subject.class, requiredMode = Schema.RequiredMode.REQUIRED)))),
            @ApiResponse(responseCode = "404", description = "Nenhuma matéria encontrada", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @GetMapping
    public ResponseEntity<List<Subject>> getSubjects(@RequestParam(required = false) String name, @RequestParam(required = false) Integer page, @RequestParam(required = false) Integer size) {
        int limit = size == null ? 10 : size;
        if (limit > 50) limit = 50;
        if (limit < 10) limit = 10;
        int offset = page == null ? 0 : (page - 1) * limit;
        if (offset < 0) offset = 0;
        Pageable pageable = PageRequest.of(offset, limit, Sort.by("name").ascending());
        List<Subject> subjects = name == null ? subjectRepository.findAll() : subjectRepository.findByNameContaining(name, pageable);

        if (subjects.isEmpty())
            return ResponseEntity.notFound().build();

        return ResponseEntity.ok(subjects);
    }

    @Operation(summary = "Criar uma matéria", description = "Cria uma matéria no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "201", description = "Matéria criada com sucesso", content = @Content),
            @ApiResponse(responseCode = "400", description = "Erro ao criar matéria", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PostMapping("/create")
    public ResponseEntity<Object> createSubject(@RequestBody @NotBlank String name) {
        if (subjectRepository.existsByName(name))
            return ResponseEntity.badRequest().body(new ErroResponseDTO("Subject already exists"));
        Subject subject = new Subject(null, name);
        subjectRepository.save(subject);
        return ResponseEntity.status(HttpStatus.CREATED).build();
    }

    @Operation(summary = "Editar uma matéria", description = "Edita uma matéria no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Matéria editada com sucesso", content = @Content(schema = @Schema(implementation = Subject.class, requiredMode = Schema.RequiredMode.REQUIRED))),
            @ApiResponse(responseCode = "400", description = "Erro ao editar matéria", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PatchMapping("/update/{id}")
    public ResponseEntity<Object> updateSubject(@PathVariable Integer id, @RequestBody String newName) {
        if (!subjectRepository.existsById(id))
            return ResponseEntity.notFound().build();

        Subject subject = subjectRepository.findById(id).orElseThrow();

        if (subjectRepository.existsByName(newName))
            return ResponseEntity.badRequest().body(new ErroResponseDTO("Subject already exists"));

        subject.setName(newName);
        subject = subjectRepository.save(subject);

        return ResponseEntity.ok(subject);
    }

    @Operation(summary = "Deletar uma matéria", description = "Deleta uma matéria no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "204", description = "Matéria deletada com sucesso", content = @Content),
            @ApiResponse(responseCode = "404", description = "Matéria não encontrada", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @DeleteMapping("/delete/{id}")
    public ResponseEntity<Void> deleteSubject(@PathVariable Integer id) {
        if (!subjectRepository.existsById(id))
            return ResponseEntity.notFound().build();

        subjectRepository.deleteById(id);
        return ResponseEntity.noContent().build();
    }
}
