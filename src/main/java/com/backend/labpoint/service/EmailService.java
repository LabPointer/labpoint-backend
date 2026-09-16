package com.backend.labpoint.service;

import com.backend.labpoint.entities.password.PasswordResetToken;
import com.backend.labpoint.repository.PasswordResetTokenRepository;

import jakarta.mail.MessagingException;
import jakarta.mail.internet.MimeMessage;

import java.time.LocalDateTime;
import java.util.Base64;
import java.util.List;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.mail.javamail.JavaMailSender;
import org.springframework.mail.javamail.MimeMessageHelper;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;
import org.thymeleaf.TemplateEngine;
import org.thymeleaf.context.Context;

@Service
public class EmailService {
    @Autowired
    private PasswordResetTokenRepository passwordResetTokenRepository;

    private final JavaMailSender mailSender;
    private final TemplateEngine templateEngine;

    @Value("${spring.mail.username}")
    private String sender;

    public EmailService(JavaMailSender mailSender, TemplateEngine templateEngine) {
        this.mailSender = mailSender;
        this.templateEngine = templateEngine;
    }

    @Scheduled(fixedDelay = 180000) // 3 minutos
    public void sendResetPasswordEmail() {
        List<PasswordResetToken> passwordResetTokens = passwordResetTokenRepository.findByPendingStatus();

        if (passwordResetTokens.isEmpty()) {
            return;
        }

        for (PasswordResetToken passResetToken : passwordResetTokens) {
            if (passResetToken.getExpiresAt().isBefore(LocalDateTime.now())) {
                passResetToken.setExpiresAt(LocalDateTime.now().plusMinutes(30));
            }

            String token = passResetToken.getId().toString();
            String encodedToken = Base64.getEncoder().encodeToString(token.getBytes());

            String link = "http://localhost:3000/reset-password?token=" + encodedToken;

            String recipient = passResetToken.getAccount().getEmail();

            Context context = new Context();
            context.setVariable("link", link);

            String htmlContent = templateEngine.process("email/reset-password", context);

            try {
                MimeMessage mimeMessage = mailSender.createMimeMessage();
                MimeMessageHelper helper = new MimeMessageHelper(mimeMessage, true, "UTF-8");

                helper.setFrom(sender);
                helper.setTo(recipient);
                helper.setSubject("Redefinição de senha - LabPoint");
                helper.setText(htmlContent, true);

                mailSender.send(mimeMessage);

                passResetToken.setStatus(com.backend.labpoint.entities.password.PasswordEmailStatusEnum.SENT);
                passwordResetTokenRepository.save(passResetToken);
                
            } catch (MessagingException e) {
                IO.println("Erro ao enviar o e-mail de redefinição de senha: " + e.getMessage());
            }
        }
    }
}