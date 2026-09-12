package com.backend.labpoint.controller;

import com.backend.labpoint.dto.auth.EmailUpdateRequestDTO;
import com.backend.labpoint.dto.auth.ForgotPasswordRequestDTO;
import com.backend.labpoint.dto.auth.SignInRequestDTO;
import com.backend.labpoint.dto.auth.SignUpRequestDTO;
import com.backend.labpoint.dto.auth.SignInCookie;
import com.backend.labpoint.dto.auth.UpdatePasswordRequestDTO;
import com.backend.labpoint.dto.error.ErroResponseDTO;
import com.backend.labpoint.entities.account.Account;
import com.backend.labpoint.exception.ResourceNotFoundException;
import com.backend.labpoint.infra.security.TokenService;
import com.backend.labpoint.repository.AccountRepository;
import com.backend.labpoint.service.AuthService;

import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.media.Schema.RequiredMode;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import tools.jackson.databind.ObjectMapper;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
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
import java.util.Base64;

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

    @Autowired
    private AccountRepository userRepository;

    @Value("${api.security.token.age}")
    private int tokenMaxAge;

    @Operation(summary = "Realizar login", description = "Realiza o login do usuário")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Login realizado com sucesso", content = @Content),
            @ApiResponse(responseCode = "403", description = "Matricula ou senha incorretos, conta desabilitada ou conta trancada", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED)))
    })
    @PostMapping("/sign-in")
    public ResponseEntity<Object> postSignIn(@RequestBody @Valid SignInRequestDTO data) {
        UsernamePasswordAuthenticationToken registrationPasswordAuthentication = new UsernamePasswordAuthenticationToken(data.registration(),
                data.password());
        Authentication auth = authenticationManager.authenticate(registrationPasswordAuthentication);
        Account user = (Account) auth.getPrincipal();
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

        ObjectMapper objectMapper = new ObjectMapper();
        SignInCookie signInCookie = new SignInCookie(user.getNickname(), user.getRole().toString());
        String json = objectMapper.writeValueAsString(signInCookie);
        String base64Value = Base64.getEncoder().encodeToString(json.getBytes());

        ResponseCookie sessionCookie = ResponseCookie
                .from("session-info", base64Value)
                .httpOnly(false)
                .secure(false)
                .path("/")
                .sameSite("Lax")
                .secure(false)
                .maxAge(maxAge)
                .build();

        return ResponseEntity
                .ok()
                .header("Set-Cookie", jwtCookie.toString())
                .header("Set-Cookie", sessionCookie.toString())
                .build();
    }

    @Operation(summary = "Atualiza os cookies do token de acesso e informações da sessão", description = "Atualiza os cookies do token de acesso e informações da sessão do usuário")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Login realizado com sucesso", content = @Content),
            @ApiResponse(responseCode = "403", description = "Matricula ou senha incorretos, conta desabilitada ou conta trancada", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class, requiredMode = RequiredMode.REQUIRED)))
    })
    @PostMapping("/refresh")
    public ResponseEntity<Object> getRefresh(@AuthenticationPrincipal UserDetails userDetails) {
        Account user = userRepository.findByRegistration(userDetails.getUsername())
                .orElseThrow(() -> new ResourceNotFoundException("Usuario nao encontrado"));
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

        ObjectMapper objectMapper = new ObjectMapper();
        SignInCookie signInCookie = new SignInCookie(user.getNickname(), user.getRole().toString());
        String json = objectMapper.writeValueAsString(signInCookie);
        String base64Value = Base64.getEncoder().encodeToString(json.getBytes());

        ResponseCookie sessionCookie = ResponseCookie
                .from("session-info", base64Value)
                .httpOnly(false)
                .secure(false)
                .path("/")
                .sameSite("Lax")
                .secure(false)
                .maxAge(maxAge)
                .build();

        return ResponseEntity
                .ok()
                .header("Set-Cookie", jwtCookie.toString())
                .header("Set-Cookie", sessionCookie.toString())
                .build();
    }

    @Operation(summary = "Realizar logout", description = "Realiza o logout do usuário")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "200", description = "Logout realizado com sucesso")
    })
    @PostMapping("/sign-out")
    public ResponseEntity<Void> postSignOut() {
        ResponseCookie deleteJwtCookie = ResponseCookie.from("jwt-session", "")
                .httpOnly(true)
                .secure(true)
                .path("/")
                .maxAge(0)
                .sameSite("Lax")
                .secure(false)
                .build();

        ResponseCookie deleteSessionCookie = ResponseCookie.from("session-info", "")
                .httpOnly(false)
                .secure(true)
                .path("/")
                .maxAge(0)
                .sameSite("Lax")
                .secure(false)
                .build();

        return ResponseEntity.ok()
                .header("Set-Cookie", deleteJwtCookie.toString())
                .header("Set-Cookie", deleteSessionCookie.toString())
                .build();
    }

    @Operation(summary = "Registrar um novo usuário", description = "Registra um novo usuário no sistema")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "201", description = "Usuário registrado com sucesso", content = @Content),
            @ApiResponse(responseCode = "400", description = "Usuário já registrado", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PostMapping("/sign-up")
    public ResponseEntity<Object> postSignUp(@AuthenticationPrincipal UserDetails userDetails, @RequestBody @Valid SignUpRequestDTO data) {
        return authService.registerNewUser(userDetails, data);
    }

    @Operation(summary = "Enviar email para redefinição de email", description = "Envia um email para o usuário redefinir o email da conta")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "202", description = "Email de redefinição enviado com sucesso", content = @Content),
            @ApiResponse(responseCode = "400", description = "E-mail inválido", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PostMapping("/request-update-email")
    public ResponseEntity<Void> postRequestUpdateEmail(@Valid @RequestBody ForgotPasswordRequestDTO data) {
        return ResponseEntity.accepted().build();
    }

    @Operation(summary = "Atualizar email do usuário", description = "Substitui o email antigo pelo novo utilizando o token de validação recebido por e-mail")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "204", description = "Email atualizado com sucesso", content = @Content),
            @ApiResponse(responseCode = "400", description = "Token inválido ou expirado ou email inválido", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PatchMapping("/update-email/{token}")
    public ResponseEntity<Void> postUpdatePassword(@AuthenticationPrincipal UserDetails userDetails, @Valid @RequestBody EmailUpdateRequestDTO data) {
        return ResponseEntity.noContent().build();
    }

    @Operation(summary = "Enviar email para redefinição de senha", description = "Envia um email para o usuário redefinir sua senha")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "202", description = "Email de redefinição de senha enviado com sucesso", content = @Content),
            @ApiResponse(responseCode = "400", description = "E-mail inválido", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PostMapping("/forgot-password")
    public ResponseEntity<Void> postForgotPassword(@Valid @RequestBody ForgotPasswordRequestDTO data) {
        return ResponseEntity.accepted().build();
    }

    @Operation(summary = "Atualizar senha do usuário", description = "Substitui a senha antiga pela nova utilizando o token de validação recebido por e-mail")
    @ApiResponses(value = {
            @ApiResponse(responseCode = "204", description = "Senha atualizada com sucesso", content = @Content),
            @ApiResponse(responseCode = "400", description = "Token inválido ou expirado ou senha inválida", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
    })
    @PatchMapping("/update-password/{token}")
    public ResponseEntity<Void> postUpdatePassword(@PathVariable @NotBlank String token, @Valid @RequestBody UpdatePasswordRequestDTO data) {
        return ResponseEntity.noContent().build();
    }
}
