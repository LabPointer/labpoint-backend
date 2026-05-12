package com.backend.labpoint.domain.error;

import org.springframework.http.HttpStatus;

public record ErroResponseDTO(HttpStatus status, String message) {

}
