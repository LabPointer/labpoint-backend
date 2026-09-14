package com.backend.labpoint.controller;

import com.backend.labpoint.dto.error.ErroResponseDTO;
import com.backend.labpoint.dto.subject.DeleteSubjectRequestDTO;
import com.backend.labpoint.dto.subject.SubjectCreateRequestDTO;
import com.backend.labpoint.dto.subject.SubjectDTO;
import com.backend.labpoint.dto.subject.SubjectRequestDTO;
import com.backend.labpoint.dto.subject.SubjectUpdateRequestDTO;
import com.backend.labpoint.entities.subject.Subject;
import com.backend.labpoint.service.SubjectService;
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
@RequestMapping("/subjects")
@Tag(name = "/subjects", description = "Endpoints para pesquisa de disciplinas")
public class SubjectController {
    @Autowired
    private SubjectService subjectService;

    @Operation(summary = "Listar todas as matérias", description = "Lista todas as matérias cadastradas")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Matérias listadas com sucesso",
                    content = @Content(array = @ArraySchema(schema = @Schema(implementation = Subject.class, requiredMode = Schema.RequiredMode.REQUIRED)))),
            @ApiResponse(responseCode = "404", description = "Nenhuma matéria encontrada", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @GetMapping
    public ResponseEntity<List<SubjectDTO>> getSubjects(@ParameterObject SubjectRequestDTO params) {
        return subjectService.getSubjects(params);
    }

    @Operation(summary = "Criar uma matéria", description = "Cria uma matéria no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "201", description = "Matéria criada com sucesso", content = @Content),
            @ApiResponse(responseCode = "400", description = "Erro ao criar matéria", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PostMapping("/manage/create")
    public ResponseEntity<Object> createSubject(@RequestBody SubjectCreateRequestDTO data) {
        subjectService.createSubject(data.name());
        return ResponseEntity.status(HttpStatus.CREATED).build();
    }

    @Operation(summary = "Editar uma matéria", description = "Edita uma matéria no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Matéria editada com sucesso", content = @Content(schema = @Schema(implementation = Subject.class, requiredMode = Schema.RequiredMode.REQUIRED))),
            @ApiResponse(responseCode = "400", description = "Erro ao editar matéria", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PatchMapping("/manage/update/{id}")
    public ResponseEntity<SubjectDTO> updateSubject(@PathVariable Long id, @RequestBody SubjectUpdateRequestDTO data) {
        return ResponseEntity.ok(subjectService.updateSubject(id, data.name()));
    }

    @Operation(summary = "Deletar uma matéria", description = "Deleta uma matéria no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "204", description = "Matéria deletada com sucesso", content = @Content),
            @ApiResponse(responseCode = "404", description = "Matéria não encontrada", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @DeleteMapping("/manage/delete")
    public ResponseEntity<Void> deleteSubject(@RequestBody DeleteSubjectRequestDTO data) {
        subjectService.deleteSubjects(data.subjectIds());
        return ResponseEntity.noContent().build();
    }
}
