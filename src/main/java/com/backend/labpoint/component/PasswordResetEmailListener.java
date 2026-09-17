package com.backend.labpoint.component;

import java.time.LocalDateTime;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.retry.annotation.Backoff;
import org.springframework.retry.annotation.Recover;
import org.springframework.retry.annotation.Retryable;
import org.springframework.scheduling.annotation.Async;
import org.springframework.stereotype.Component;
import org.springframework.transaction.event.TransactionPhase;
import org.springframework.transaction.event.TransactionalEventListener;

import com.backend.labpoint.entities.password.PasswordEmailStatusEnum;
import com.backend.labpoint.entities.password.PasswordResetToken;
import com.backend.labpoint.event.password.PasswordResetTokenRequestedEvent;
import com.backend.labpoint.repository.PasswordResetTokenRepository;
import com.backend.labpoint.service.EmailService;

import jakarta.mail.MessagingException;

@Component
public class PasswordResetEmailListener {
    @Autowired
    private PasswordResetTokenRepository tokenRepository;
 
    @Autowired
    private EmailService emailService;
 
    @Async
    @TransactionalEventListener(phase = TransactionPhase.AFTER_COMMIT)
    @Retryable(
            retryFor = MessagingException.class,
            maxAttempts = 3,
            backoff = @Backoff(delay = 2000, multiplier = 2)
    )
    public void handlePasswordResetRequested(PasswordResetTokenRequestedEvent event) throws MessagingException {
        PasswordResetToken token = tokenRepository.findById(event.passwordResetId())
                .orElseThrow();
 
        emailService.sendResetPasswordEmail(token);
 
        token.setStatus(PasswordEmailStatusEnum.SENT);
        token.setUsedAt(LocalDateTime.now());
        tokenRepository.save(token);
    }
 
    @Recover
    public void recover(MessagingException e, PasswordResetTokenRequestedEvent event) {
        tokenRepository.findById(event.passwordResetId()).ifPresent(token -> {
            token.setStatus(PasswordEmailStatusEnum.PENDING);
            tokenRepository.save(token);
        });
    }
}
