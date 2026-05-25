package com.backend.labpoint.controller;

import com.backend.labpoint.domain.error.ErroResponseDTO;
import com.backend.labpoint.domain.user.*;
import com.backend.labpoint.infra.security.TokenService;
import com.backend.labpoint.repository.UserRepository;
import com.backend.labpoint.service.AuthService;
import com.backend.labpoint.specification.AuthSpecification;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.ArraySchema;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Pageable;
import org.springframework.data.domain.Sort;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseCookie;
import org.springframework.http.ResponseEntity;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.security.core.userdetails.UsernameNotFoundException;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Controller;
import org.springframework.data.domain.Pageable;
import org.springframework.web.bind.annotation.*;

import java.time.Duration;
import java.time.Instant;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;

@Controller
@RequestMapping("/auth")
@Tag(name = "/auth", description = "Endpoints para gerenciamento de autenticação")
public class AuthController {

        @Autowired
        private AuthService authService;

        @Autowired
        private AuthenticationManager authenticationManager;

        @Autowired
        private UserRepository usersRepository;

        @Autowired
        private TokenService tokenService;

        @Value("${api.security.token.age}")
        private long tokenMaxAge;

        @Operation(summary = "Login", description = "Realiza o login do usuário")
        @ApiResponses(value = {
                @ApiResponse(responseCode = "200", description = "Login realizado com sucesso", content = @Content(array = @ArraySchema(schema = @Schema(implementation = UserRequestDTO.class, requiredMode = Schema.RequiredMode.REQUIRED)))),
                @ApiResponse(responseCode = "404", description = "Usuário nao encontrado", content = @Content)
        })
        @GetMapping("/users")
        public ResponseEntity<List<UserResponseDTO>> getUsers(@RequestParam UserRequestDTO params) {
                var spec = AuthSpecification.filters(params.username(), params.email(), params.registration(), params.role());

                long total = authService.countUsers(spec);

                int offset = params.offset() != null ? params.offset() : 0;
                int limit = params.limit() != null ? params.limit() : 10;
                Pageable pageable = PageRequest.of(offset / limit, limit, Sort.by("registration").ascending());

                var users = authService.getUsers(spec, pageable);

                if (users == null || users.isEmpty()) return ResponseEntity.notFound().build();

                var userList = new ArrayList<UserResponseDTO>();
                users.forEach(u -> {
                        var user = new UserResponseDTO(u.getId(), u.getRegistration(), u.getUsername(), u.getEmail(), u.getRole(), u.isEnabled(), offset, limit, (int) total);
                        userList.add(user);
                });

                return ResponseEntity.ok(userList);
        }

        @Operation(summary = "Login", description = "Realiza o login do usuário")
        @ApiResponses(value = {
                        @ApiResponse(responseCode = "200", description = "Login realizado com sucesso", content = @Content(schema = @Schema(implementation = LoginResponseDTO.class, requiredMode = Schema.RequiredMode.REQUIRED))),
                        @ApiResponse(responseCode = "401", description = "Usuário ou senha incorretos", content = @Content)
        })
        @PostMapping("/login")
        public ResponseEntity<LoginResponseDTO> postLogin(@RequestBody @Valid LoginRequestDTO data) {
                var registrationPasswordAuthentication = new UsernamePasswordAuthenticationToken(data.registration(),
                                data.password());
                var auth = authenticationManager.authenticate(registrationPasswordAuthentication);
                var user = (User) auth.getPrincipal();
                var token = tokenService.generateToken(user);

                var maxAge = Duration.ofHours(tokenMaxAge);

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

        @Operation(summary = "Logout", description = "Realiza o logout do usuário")
        @ApiResponses(value = {
                        @ApiResponse(responseCode = "200", description = "Logout realizado com sucesso")
        })
        @PostMapping("/logout")
        public ResponseEntity<Void> PostLogout() {
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

        @Operation(summary = "Registra um novo usuário", description = "Registra um novo usuário no sistema")
        @ApiResponses(value = {
                @ApiResponse(responseCode = "201", description = "Usuário registrado com sucesso", content = @Content),
                @ApiResponse(responseCode = "400", description = "Usuário já registrado", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
        })
        @PostMapping("/register")
        public ResponseEntity<Object> postRegister(@RequestBody @Valid RegisterRequestDTO data) {
                if (usersRepository.findByRegistration(data.registration()).isPresent()) {
                        return ResponseEntity.badRequest().body(
                                new ErroResponseDTO(
                                        HttpStatus.BAD_REQUEST,
                                        "Usuario ja registrado"));
                }

                var encryptedPass = new BCryptPasswordEncoder().encode(data.password());
                var user = new User(data.username(), data.email(), data.registration(), encryptedPass, data.role());

                usersRepository.save(user);

                return ResponseEntity.status(HttpStatus.CREATED).build();
        }
/*
        @Operation(summary = "Registra um novo usuário", description = "Registra um novo usuário no sistema")
        @ApiResponses(value = {
                @ApiResponse(responseCode = "204", description = "Usuário editado com sucesso", content = @Content),
                @ApiResponse(responseCode = "400", description = "Usuário já registrado", content = @Content(schema = @Schema(implementation = ErroResponseDTO.class)))
        })
        @PatchMapping("/update")
        public ResponseEntity<ErroResponseDTO> patchUpdate(@AuthenticationPrincipal UserDetails userDetails, @RequestBody @Valid UserUpdateRequestDTO body) {
                var user = usersRepository.findByRegistration(userDetails.getUsername()).orElseThrow();
                var uuid = body.uuid() == null ? user.getId() : body.uuid();
                if (uuid != user.getId() && (user.getRole() == UserRole.OWNER || user.getRole() == UserRole.ADMIN)) {
                        return ResponseEntity.status(HttpStatus.FORBIDDEN).build();
                }


        }
 */
}
