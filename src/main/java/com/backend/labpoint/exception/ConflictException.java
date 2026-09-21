package com.backend.labpoint.exception;

import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.ResponseStatus;

@ResponseStatus(code = HttpStatus.CONFLICT, reason = "Conflict")
public class ConflictException extends RuntimeException {
    public ConflictException(String message) {
        super(message);
    }
}