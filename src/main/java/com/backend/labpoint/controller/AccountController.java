package com.backend.labpoint.controller;

import java.util.List;

import org.springdoc.core.annotations.ParameterObject;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.RequestMapping;

import com.backend.labpoint.dto.error.ErroResponseDTO;
import com.backend.labpoint.dto.user.ManageUserRequestDTO;
import com.backend.labpoint.dto.user.ManageUserResponseDTO;
import com.backend.labpoint.dto.user.ManageUserUpdateRequestDTO;
import com.backend.labpoint.service.AccountService;

import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.ArraySchema;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.media.Schema.RequiredMode;
import io.swagger.v3.oas.annotations.parameters.RequestBody;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;

@Controller
@RequestMapping("/account")
@Tag(name = "/account", description = "Endpoints para gerenciamento de contas")
public class AccountController {

    @Autowired
    private AccountService userService;

    @Operation(summary = "Pesquisar por usuarios", description = "Filtra e retorna usuarios encontrados. OBS: A rota funciona apenas para admins")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Retorna lista de usuarios encontrados", content = @Content(array = @ArraySchema(schema = @Schema(implementation = ManageUserRequestDTO.class, requiredMode = Schema.RequiredMode.REQUIRED)))),
            @ApiResponse(responseCode = "404", description = "Usuário nao encontrado", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED)))
    })
    @GetMapping("/manage/search")
    public ResponseEntity<List<ManageUserResponseDTO>> getUsers(@ParameterObject ManageUserRequestDTO params) {
        return userService.getAccount(params);
    }

    @Operation(summary = "Atualizar as informações do usuario", description = "Atualiza as informações do usuario no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "204", description = "Usuário atualizado com sucesso", content = @Content),
            @ApiResponse(responseCode = "400", description = "Usuário não encontrado", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PatchMapping("/manage/update")
    public ResponseEntity<Void> patchUpdate(@RequestBody @Valid ManageUserUpdateRequestDTO data) {
        return userService.updateAccountInfo(data);
    }
}
