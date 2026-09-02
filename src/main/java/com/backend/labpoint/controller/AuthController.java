package com.backend.labpoint.controller;

import com.backend.labpoint.domain.error.ErroResponseDTO;
import com.backend.labpoint.domain.user.*;
import com.backend.labpoint.exception.ResourceNotFoundException;
import com.backend.labpoint.infra.security.TokenService;
import com.backend.labpoint.service.AuthService;
import com.backend.labpoint.specification.AuthSpecification;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.ArraySchema;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.media.Schema.RequiredMode;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotBlank;

import org.springdoc.core.annotations.ParameterObject;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Pageable;
import org.springframework.data.domain.Sort;
import org.springframework.data.jpa.domain.Specification;
import org.springframework.http.ResponseCookie;
import org.springframework.http.ResponseEntity;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.*;

import java.time.Duration;
import java.time.Instant;
import java.util.ArrayList;
import java.util.List;

@Controller
@RequestMapping("/auth")
@Tag(name = "/auth", description = "Endpoints para gerenciamento de autenticação")
public class AuthController {

    @Autowired
    private AuthService authService;

    @Autowired
    private AuthenticationManager authenticationManager;

    @Autowired
    private TokenService tokenService;

    @Value("${api.security.token.age}")
    private int tokenMaxAge;

    @Operation(summary = "Pesquisar por usuarios", description = "Filtra e retorna usuarios encontrados. OBS: A rota funciona apenas para admins")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Retorna lista de usuarios encontrados", content = @Content(array = @ArraySchema(schema = @Schema(implementation = UserRequestDTO.class, requiredMode = Schema.RequiredMode.REQUIRED)))),
            @ApiResponse(responseCode = "404", description = "Usuário nao encontrado", content = @Content)
    })
    @GetMapping("/users")
    public ResponseEntity<List<UserResponseDTO>> getUsers(@ParameterObject UserRequestDTO params) {
        Specification<User> spec = AuthSpecification.filters(params.username(), params.email(), params.registration(),
                params.role());

        int offset = params.offset() != null ? params.offset() : 0;
        int limit = params.limit() != null ? params.limit() : 10;
        Pageable pageable = PageRequest.of(offset / limit, limit, Sort.by("registration").ascending());

        List<User> users = authService.getUsers(spec, pageable);

        // long total = authService.countUsers(spec);
        int total = users.size();

        ArrayList<UserResponseDTO> userList = new ArrayList<UserResponseDTO>();
        for (User u : users) {
            UserResponseDTO user = new UserResponseDTO(u.getId(), u.getRegistration(), u.getUsername(), u.getEmail(),
                    u.getRole(), u.isEnabled(),
                    offset, limit, total);
            userList.add(user);
        }

        return ResponseEntity.ok(userList);
    }

    @Operation(summary = "Realizar login", description = "Realiza o login do usuário")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Login realizado com sucesso", content = @Content(schema = @Schema(implementation = LoginResponseDTO.class, requiredMode = Schema.RequiredMode.REQUIRED))),
            @ApiResponse(responseCode = "403", description = "Matricula ou senha incorretos, conta desabilitada ou conta trancada", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED)))
    })
    @PostMapping("/sign-in")
    public ResponseEntity<LoginResponseDTO> postSignIn(@RequestBody @Valid LoginRequestDTO data) {
        UsernamePasswordAuthenticationToken registrationPasswordAuthentication = new UsernamePasswordAuthenticationToken(data.registration(),
                data.password());
        Authentication auth = authenticationManager.authenticate(registrationPasswordAuthentication);
        User user = (User) auth.getPrincipal();
        if (user == null)
            throw new ResourceNotFoundException("Usuario nao encontrado");
        String token = tokenService.generateToken(user);

        Duration maxAge = Duration.ofHours(tokenMaxAge);

        ResponseCookie jwtCookie = ResponseCookie
                .from("jwt-session", token)
                .httpOnly(true)
                .secure(false)
                .path("/")
                .sameSite("Lax")
                .secure(false)
                .maxAge(maxAge)
                .build();

        return ResponseEntity
                .ok()
                .header("Set-Cookie", jwtCookie.toString())
                .body(new LoginResponseDTO(user.getUsername(), user.getRole().toString(),
                        Instant.now().plus(maxAge).toEpochMilli()));
    }

    @Operation(summary = "Realizar logout", description = "Realiza o logout do usuário")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Logout realizado com sucesso")
    })
    @PostMapping("/sign-out")
    public ResponseEntity<Void> postSignOut() {
        ResponseCookie deleteCookie = ResponseCookie.from("jwt-session", "")
                .httpOnly(true)
                .secure(true)
                .path("/")
                .maxAge(0)
                .sameSite("Lax")
                .secure(false)
                .build();

        return ResponseEntity.ok().header("Set-Cookie", deleteCookie.toString()).build();
    }

    @Operation(summary = "Registrar um novo usuário", description = "Registra um novo usuário no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "201", description = "Usuário registrado com sucesso", content = @Content),
            @ApiResponse(responseCode = "400", description = "Usuário já registrado", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PostMapping("/sign-up")
    public ResponseEntity<?> postSignUp(@AuthenticationPrincipal UserDetails userDetails, @RequestBody @Valid RegisterRequestDTO data) {
        return authService.registerNewUser(userDetails, data);
    }

    @Operation(summary = "Atualizar as informações do usuario", description = "Atualiza as informações do usuario no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Usuário registrado com sucesso", content = @Content(schema = @Schema(implementation = UserUpdateResponseDTO.class, requiredMode = Schema.RequiredMode.REQUIRED))),
            @ApiResponse(responseCode = "400", description = "Usuário já registrado", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PatchMapping("/update")
    public ResponseEntity<?> patchUpdate(@AuthenticationPrincipal UserDetails userDetails, @RequestBody @Valid UserUpdateRequestDTO data) {
        return authService.updateUserInfo(userDetails, data);
    }

    @PostMapping("/reset-password")
    public ResponseEntity<?> postResetPassword(@NotBlank @Email String email) {
        return ResponseEntity.ok().build();
    }

    @PatchMapping("/update-password")
    public ResponseEntity<?> postUpdatePassword(@NotBlank String token, @NotBlank String password) {
        return ResponseEntity.ok().build();
    }
}
