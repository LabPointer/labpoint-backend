package com.backend.labpoint.service;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.security.SecureRandom;
import java.time.LocalDateTime;
import java.util.Base64;
import java.util.Optional;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import com.backend.labpoint.entities.resetpasswordtoken.PasswordResetToken;
import com.backend.labpoint.entities.user.User;
import com.backend.labpoint.repository.PasswordResetTokenRepository;
import com.backend.labpoint.repository.UserRepository;

import jakarta.transaction.Transactional;

@Service
public class PasswordResetService {

    @Autowired 
    private UserRepository userRepository;

    @Autowired 
    private PasswordResetTokenRepository tokenRepository;

    @Autowired 
    private EmailService emailService;

    private static final int TOKEN_BYTES = 32;
    private static final int EXPIRACAO_MINUTOS = 30;

    @Transactional
    public void solicitarReset(String email) {
        Optional<User> usuarioOpt = userRepository.findByEmail(email);

        // Não revela se o email existe ou não
        if (usuarioOpt.isEmpty()) {
            return;
        }

        User usuario = usuarioOpt.get();

        // Invalida tokens anteriores não usados (evita múltiplos válidos ao mesmo tempo)
        // TODO: Implementar a lógica para invalidar tokens anteriores
        //tokenRepository.invalidarTokensAnteriores(usuario.getId());

        // Gera token aleatório seguro
        String tokenPuro = gerarTokenSeguro();
        String tokenHash = hashToken(tokenPuro);

        PasswordResetToken resetToken = new PasswordResetToken();
        resetToken.setUser(usuario);
        resetToken.setTokenHash(tokenHash);
        resetToken.setExpiresAt(LocalDateTime.now().plusMinutes(EXPIRACAO_MINUTOS));
        tokenRepository.save(resetToken);

        String link = "https://labpoint.senai.br/redefinir-senha?token=" + tokenPuro;
        //emailService.enviarEmailResetSenha(usuario.getEmail(), usuario.getNome(), link);
    }

    private String gerarTokenSeguro() {
        SecureRandom random = new SecureRandom();
        byte[] bytes = new byte[TOKEN_BYTES];
        random.nextBytes(bytes);
        return Base64.getUrlEncoder().withoutPadding().encodeToString(bytes);
    }

    private String hashToken(String token) {
        try {
            MessageDigest digest = MessageDigest.getInstance("SHA-256");
            byte[] hash = digest.digest(token.getBytes(StandardCharsets.UTF_8));
            return Base64.getEncoder().encodeToString(hash);
        } catch (NoSuchAlgorithmException e) {
            throw new RuntimeException(e);
        }
    }
}
