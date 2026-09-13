package com.backend.labpoint.service;

import com.backend.labpoint.exception.InternalServerException;
import jakarta.mail.MessagingException;
import jakarta.mail.internet.MimeMessage;
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

    public void sendResetPasswordEmail(String recipient, String token) {
        String link = "http://localhost:3000/reset-password?token=" + token;

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
        } catch (MessagingException e) {
            throw new InternalServerException("Erro ao enviar email de redefinição de senha: " + e.getMessage());
        }
    }
}