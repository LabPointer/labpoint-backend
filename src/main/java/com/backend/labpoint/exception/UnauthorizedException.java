package com.backend.labpoint.exception;

import lombok.Getter;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.ResponseStatus;

@ResponseStatus(code = HttpStatus.UNAUTHORIZED, reason = "Unauthorized")
@Getter
public class UnauthorizedException extends RuntimeException {
    private boolean logout;
    public UnauthorizedException(String message, boolean logout) {
        super(message);
        this.logout = logout;
    }
}
