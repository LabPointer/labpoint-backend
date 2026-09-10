package com.backend.labpoint.exception;

import lombok.Getter;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.ResponseStatus;

@ResponseStatus(code = HttpStatus.FORBIDDEN, reason = "Forbidden")
@Getter
public class ForbiddenException extends RuntimeException {
    private boolean logout;
    public ForbiddenException(String message, boolean logout) {
        super(message);
        this.logout = logout;
    }
}
