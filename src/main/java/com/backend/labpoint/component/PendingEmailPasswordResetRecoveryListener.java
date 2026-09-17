package com.backend.labpoint.component;

import com.backend.labpoint.entities.password.PasswordResetToken;
import com.backend.labpoint.event.password.PasswordResetTokenRequestedEvent;
import com.backend.labpoint.repository.PasswordResetTokenRepository;
 
import java.time.LocalDateTime;
import java.util.List;
 
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.context.event.EventListener;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.stereotype.Component;

@Component
public class PendingEmailPasswordResetRecoveryListener {
    private static final Logger log = LoggerFactory.getLogger(PendingEmailPasswordResetRecoveryListener.class);
 
    @Autowired
    private PasswordResetTokenRepository tokenRepository;
 
    @Autowired
    private ApplicationEventPublisher eventPublisher;
 
    @EventListener(ApplicationReadyEvent.class)
    public void reprocessarPendentes() {
        List<PasswordResetToken> pendentes = tokenRepository.findByPendingStatus();
 
        if (pendentes.isEmpty()) {
            log.info("Nenhuma notificação pendente encontrada na inicialização.");
            return;
        }
 
        log.info("Encontradas {} notificações pendentes. Reprocessando...", pendentes.size());
 
        for (PasswordResetToken token : pendentes) {
            if (token.getExpiresAt().isBefore(LocalDateTime.now())) {
                log.info("Token {} expirado, ignorando reenvio.", token.getId());
                continue;
            }
 
            eventPublisher.publishEvent(new PasswordResetTokenRequestedEvent(token.getId()));
        }
    }
}
