package com.backend.labpoint.dto.auth;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record UpdatePasswordRequestDTO(
    @NotBlank(message = "A senha é obrigatória")
    @Size(min = 5, message = "A senha deve ter no mínimo 5 caracteres")
    String password
) {}