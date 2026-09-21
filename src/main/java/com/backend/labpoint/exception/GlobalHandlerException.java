package com.backend.labpoint.exception;

import com.backend.labpoint.dto.error.ErroResponseDTO;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.authentication.BadCredentialsException;
import org.springframework.security.authentication.DisabledException;
import org.springframework.security.authentication.LockedException;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

@ControllerAdvice
public class GlobalHandlerException {
    @ExceptionHandler(ResourceNotFoundException.class)
    public ResponseEntity<ErroResponseDTO> handleResourceNotFoundException(ResourceNotFoundException e) {
        return ResponseEntity.status(HttpStatus.NOT_FOUND).body(new ErroResponseDTO(e.getMessage(), false));
    }

    @ExceptionHandler(BadRequestException.class)
    public ResponseEntity<ErroResponseDTO> handleBadRequestException(BadRequestException e) {
        return ResponseEntity.status(HttpStatus.BAD_REQUEST).body(new ErroResponseDTO(e.getMessage(), false));
    }

    @ExceptionHandler(ForbiddenException.class)
    public ResponseEntity<ErroResponseDTO> handleForbiddenException(ForbiddenException e) {
        return ResponseEntity.status(HttpStatus.FORBIDDEN).body(new ErroResponseDTO(e.getMessage(), e.isLogout()));
    }

    @ExceptionHandler(UnauthorizedException.class)
    public ResponseEntity<ErroResponseDTO> handleUnauthorizedException(UnauthorizedException e) {
        return ResponseEntity.status(HttpStatus.UNAUTHORIZED).body(new ErroResponseDTO(e.getMessage(), e.isLogout()));
    }

    @ExceptionHandler(DisabledException.class)
    public ResponseEntity<ErroResponseDTO> handleDisabledException(DisabledException e) {
        return ResponseEntity.status(HttpStatus.FORBIDDEN).body(new ErroResponseDTO(e.getMessage(), false));
    }

    @ExceptionHandler(LockedException.class)
    public ResponseEntity<ErroResponseDTO> handleLockedException(LockedException e) {
        return ResponseEntity.status(HttpStatus.FORBIDDEN).body(new ErroResponseDTO(e.getMessage(), false));
    }

    @ExceptionHandler(BadCredentialsException.class)
    public ResponseEntity<ErroResponseDTO> handleLockedException(BadCredentialsException e) {
        return ResponseEntity.status(HttpStatus.FORBIDDEN).body(new ErroResponseDTO(e.getMessage(), false));
    }
    @ExceptionHandler(ConflictException.class)
    public ResponseEntity<ErroResponseDTO> handleConflictException(ConflictException e) {
        return ResponseEntity.status(HttpStatus.CONFLICT).body(new ErroResponseDTO(e.getMessage(), false));
    }

    @ExceptionHandler(InternalServerException.class)
    public ResponseEntity<ErroResponseDTO> handleInternalServerException(InternalServerException e) {
        return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body(new ErroResponseDTO(e.getMessage(), false));
    }
}
