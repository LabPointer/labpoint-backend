package com.backend.labpoint.controller;

import com.backend.labpoint.domain.users.Users;
import com.backend.labpoint.dto.body.AuthRequestDTO;
import com.backend.labpoint.dto.body.RegisterRequestDTO;
import com.backend.labpoint.dto.response.LoginResponseDTO;
import com.backend.labpoint.infra.security.TokenService;
import com.backend.labpoint.repository.UsersRepository;
import jakarta.validation.Valid;

import java.net.http.HttpHeaders;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseCookie;
import org.springframework.http.ResponseEntity;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.*;

@Controller
@RequestMapping("/auth")
@CrossOrigin
public class AuthController {

    @Autowired
    private AuthenticationManager authenticationManager;

    @Autowired
    private UsersRepository usersRepository;

    @Autowired
    private TokenService tokenService;

    @PostMapping("/login")
    public ResponseEntity<LoginResponseDTO> login(@RequestBody @Valid AuthRequestDTO data) {
        var registrationPasswordAuthentication = new UsernamePasswordAuthenticationToken(data.registration(), data.password());
        var auth = authenticationManager.authenticate(registrationPasswordAuthentication);

        var token = tokenService.generateToken((Users) auth.getPrincipal());

        ResponseCookie cookie = ResponseCookie.from("session_jwt", token)
                .httpOnly(true)
                .secure(true)
                .path("/")
                .build();

        return ResponseEntity.ok().header("Set-Cookie", cookie.toString()).build();
    }

    @PostMapping("/register")
    public ResponseEntity<Void> register(@RequestBody@Valid RegisterRequestDTO data) {
        if (usersRepository.findByRegistration(data.registration()).isPresent())
            return ResponseEntity.badRequest().build();

        var encryptedPass = new BCryptPasswordEncoder().encode(data.password());
        var user = new Users(data.username(), data.email(), data.registration(), encryptedPass, data.role());

        usersRepository.save(user);

        return ResponseEntity.ok().build();
    }
}
