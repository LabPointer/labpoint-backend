package com.backend.labpoint.domain.user;

import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;

public record RegisterRequestDTO(
        @NotEmpty(message = "Campo username nao pode ser nulo ou vazio!")
        @Size(min = 5, max = 32)
        String username,
        @NotEmpty(message = "Campo username nao pode ser nulo ou vazio!")
        @Email(message = "Texto nao é um email valido!")
        @Size(max = 320)
        String email,
        @NotEmpty(message = "Campo username nao pode ser nulo ou vazio!")
        @Size(min = 5, max = 16)
        String registration,
        @NotEmpty(message = "Campo username nao pode ser nulo ou vazio!")
        @Size(min = 5, max = 16)
        String password,
        @NotNull(message = "Campo username nao pode ser nulo ou vazio!")
        UserRole role,
        Boolean enabled) {
}
