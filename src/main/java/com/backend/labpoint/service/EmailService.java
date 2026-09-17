package com.backend.labpoint.service;

import com.backend.labpoint.entities.password.PasswordResetToken;

import jakarta.mail.MessagingException;
import jakarta.mail.internet.MimeMessage;

import java.util.Base64;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.mail.javamail.JavaMailSender;
import org.springframework.mail.javamail.MimeMessageHelper;
import org.springframework.stereotype.Service;
import org.thymeleaf.TemplateEngine;
import org.thymeleaf.context.Context;

@Service
public class EmailService {

    private final JavaMailSender mailSender;
    private final TemplateEngine templateEngine;

    @Value("${spring.mail.username}")
    private String sender;

    public EmailService(JavaMailSender mailSender, TemplateEngine templateEngine) {
        this.mailSender = mailSender;
        this.templateEngine = templateEngine;
    }

    public void sendResetPasswordEmail(PasswordResetToken passResetToken) throws MessagingException {
        String encodedToken = Base64.getEncoder().encodeToString(
                passResetToken.getId().toString().getBytes());
        String link = "http://localhost:3000/reset-password?token=" + encodedToken;
        String recipient = passResetToken.getAccount().getEmail();

        Context context = new Context();
        context.setVariable("link", link);
        String htmlContent = templateEngine.process("email/reset-password", context);

        MimeMessage mimeMessage = mailSender.createMimeMessage();
        MimeMessageHelper helper = new MimeMessageHelper(mimeMessage, true, "UTF-8");
        helper.setFrom(sender);
        helper.setTo(recipient);
        helper.setSubject("Redefinição de senha - LabPoint");
        helper.setText(htmlContent, true);

        mailSender.send(mimeMessage);
    }
}