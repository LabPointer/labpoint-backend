package com.backend.labpoint.controller;

import java.util.List;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import com.backend.labpoint.domain.error.ErroResponseDTO;
import com.backend.labpoint.domain.subject.Subject;
import com.backend.labpoint.repository.SubjectRepository;

import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.media.ArraySchema;
import io.swagger.v3.oas.annotations.parameters.RequestBody;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import jakarta.validation.constraints.NotBlank;

@RestController
@RequestMapping("/subjects")
public class SubjectController {

    @Autowired
    private SubjectRepository subjectRepository;

    @Operation(summary = "Lista todas as matérias", description = "Lista todas as matérias cadastradas")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Matérias listadas com sucesso", 
                content = @Content(array = @ArraySchema(schema = @Schema(implementation = Subject.class, requiredMode = Schema.RequiredMode.REQUIRED)))),
            @ApiResponse(responseCode = "404", description = "Nenhuma matéria encontrada", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @GetMapping
    public ResponseEntity<List<Subject>> getSubjects(@RequestParam(required = false) String name) {
        List<Subject> subjects = name == null ? subjectRepository.findAll() : subjectRepository.findByNameContaining(name);

        if (subjects.isEmpty()) 
            return ResponseEntity.notFound().build();

        return ResponseEntity.ok(subjects);
    }

    @Operation(summary = "Cria uma matéria", description = "Cria uma matéria no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "201", description = "Matéria criada com sucesso", content = @Content),
            @ApiResponse(responseCode = "400", description = "Erro ao criar matéria", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PostMapping("/create")
    public ResponseEntity createSubject(@RequestBody @NotBlank String name) {
        if (subjectRepository.existsByName(name))
            return ResponseEntity.badRequest().body(new ErroResponseDTO(HttpStatus.BAD_REQUEST, "Subject already exists"));
        Subject subject = new Subject(null, name);
        subject = subjectRepository.save(subject);
        return ResponseEntity.status(HttpStatus.CREATED).build();
    }

    @Operation(summary = "Edita uma matéria", description = "Edita uma matéria no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Matéria editada com sucesso", content = @Content(schema = @Schema(implementation = Subject.class, requiredMode = Schema.RequiredMode.REQUIRED))),
            @ApiResponse(responseCode = "400", description = "Erro ao editar matéria", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PatchMapping("/update/{id}")
    public ResponseEntity<Object> updateSubject(@PathVariable Integer id, @RequestBody String newName) {
        if (!subjectRepository.existsById(id))
            return ResponseEntity.notFound().build();

        var subject = subjectRepository.findById(id).orElseThrow();

        if (subjectRepository.existsByName(newName))
            return ResponseEntity.badRequest().body(new ErroResponseDTO(HttpStatus.BAD_REQUEST, "Subject already exists"));

        subject.setName(newName);
        subject = subjectRepository.save(subject);

        return ResponseEntity.ok(subject);
    }

    @Operation(summary = "Deleta uma matéria", description = "Deleta uma matéria no sistema")
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
