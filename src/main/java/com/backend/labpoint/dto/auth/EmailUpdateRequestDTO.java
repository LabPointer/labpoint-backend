package com.backend.labpoint.dto.auth;

import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.Size;

public record EmailUpdateRequestDTO(
    @Email(message = "Texto nao é um email valido!")
    @Size(max = 320)
    String email
) {

}
