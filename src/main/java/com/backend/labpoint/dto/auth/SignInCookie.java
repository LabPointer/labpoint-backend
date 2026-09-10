package com.backend.labpoint.dto.auth;

import jakarta.validation.constraints.NotEmpty;

public record SignInCookie(@NotEmpty String username, @NotEmpty String role) {
    
}
