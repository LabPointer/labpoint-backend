package com.backend.labpoint.domain.user;

import io.swagger.v3.oas.annotations.media.Schema;
import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.Size;

public record LoginRequestDTO(
    @NotEmpty(message = "Matricula nao pode ser nula ou vazia!")
    @Size(min = 5, max = 16)
    String registration,
    @NotEmpty(message = "Senha nao pode ser nula ou vazia!")
    @Size(min = 5, max = 16)
    String password) {
}
