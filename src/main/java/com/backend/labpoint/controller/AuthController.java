package com.backend.labpoint.controller;

import com.backend.labpoint.domain.error.ErroResponseDTO;
import com.backend.labpoint.domain.users.LoginRequestDTO;
import com.backend.labpoint.domain.users.RegisterRequestDTO;
import com.backend.labpoint.domain.users.Users;
import com.backend.labpoint.infra.security.TokenService;
import com.backend.labpoint.repository.UsersRepository;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseCookie;
import org.springframework.http.ResponseEntity;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.CrossOrigin;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;

@Controller
@RequestMapping("/auth")
@Tag(name = "/auth", description = "Endpoints para gerenciamento de autenticação")
@CrossOrigin
public class AuthController {

    @Autowired
    private AuthenticationManager authenticationManager;

    @Autowired
    private UsersRepository usersRepository;

    @Autowired
    private TokenService tokenService;

    @Operation(summary = "Login", description = "Realiza o login do usuário")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Login realizado com sucesso"),
            @ApiResponse(responseCode = "401", description = "Usuário ou senha incorretos")
    })
    @PostMapping("/login")
    public ResponseEntity<Void> login(@RequestBody @Valid LoginRequestDTO data) {
        var registrationPasswordAuthentication = new UsernamePasswordAuthenticationToken(data.registration(), data.password());
        var auth = authenticationManager.authenticate(registrationPasswordAuthentication);

        var token = tokenService.generateToken((Users) auth.getPrincipal());

        ResponseCookie cookie = ResponseCookie.from("session_jwt", token)
                .httpOnly(true)
                .secure(false) // Permite o envio do cookie via HTTP (localhost)
                .path("/")
                .sameSite("Lax")
                .build();

        return ResponseEntity.ok().header("Set-Cookie", cookie.toString()).build();
    }

    @Operation(summary = "Logout", description = "Realiza o logout do usuário")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Logout realizado com sucesso")
    })
    @PostMapping("/logout")
    public ResponseEntity<Void> logout() {
        ResponseCookie deleteCookie = ResponseCookie.from("session_jwt", "")
                .httpOnly(true)
                .secure(true)
                .path("/")
                .maxAge(0)
                .sameSite("Strict")
                .build();

        return ResponseEntity.ok().header("Set-Cookie", deleteCookie.toString()).build();
    }

    @Operation(summary = "Registra um novo usuário", description = "Registra um novo usuário no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "204", description = "Usuário registrado com sucesso", content = @Content),
            @ApiResponse(responseCode = "400", description = "Usuário já registrado", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PostMapping("/register")
    public ResponseEntity<ErroResponseDTO> register(@RequestBody @Valid RegisterRequestDTO data) {
        if (usersRepository.findByRegistration(data.registration()).isPresent()) {
            return ResponseEntity.badRequest().body(
                    new ErroResponseDTO(
                            HttpStatus.BAD_REQUEST,
                            "Usuario ja registrado"
                    )
            );
        }

        var encryptedPass = new BCryptPasswordEncoder().encode(data.password());
        var user = new Users(data.username(), data.email(), data.registration(), encryptedPass, data.role());

        usersRepository.save(user);

        return ResponseEntity.noContent().build();
    }
}
