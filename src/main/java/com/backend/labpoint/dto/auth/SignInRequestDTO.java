package com.backend.labpoint.dto.auth;

import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.Size;

public record SignInRequestDTO(
        @NotEmpty(message = "Matricula nao pode ser nula ou vazia!")
        @Size(min = 5, max = 16)
        String registration,
        @NotEmpty(message = "Senha nao pode ser nula ou vazia!")
        @Size(min = 5, max = 16)
        String password) {
}
