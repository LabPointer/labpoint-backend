package com.backend.labpoint.service;

import java.nio.charset.StandardCharsets;
import java.time.LocalDateTime;
import java.util.Base64;
import java.util.Optional;
import java.util.UUID;

import com.backend.labpoint.entities.password.PasswordResetToken;
import com.backend.labpoint.exception.BadRequestException;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Service;

import com.backend.labpoint.entities.account.Account;
import com.backend.labpoint.repository.PasswordResetTokenRepository;
import com.backend.labpoint.repository.AccountRepository;

import jakarta.transaction.Transactional;

@Service
public class PasswordResetService {

    @Autowired 
    private AccountRepository accountRepository;

    @Autowired 
    private PasswordResetTokenRepository passwordResetTokenRepository;

    private static final int TOKEN_BYTES = 32;
    private static final int EXPIRACAO_MINUTOS = 30;

    @Transactional
    public String createRequest(String email) {
        Optional<Account> accountOpt = accountRepository.findByEmail(email);

        if (accountOpt.isEmpty()) {
            return null;
        }

        Account account = accountOpt.get();

        PasswordResetToken passResetToken = new PasswordResetToken(null, account, LocalDateTime.now().plusMinutes(EXPIRACAO_MINUTOS), null);
        passResetToken = passwordResetTokenRepository.save(passResetToken);

        String token = passResetToken.getId().toString();

        return Base64.getEncoder().encodeToString(token.getBytes());
    }

    public void resetPassword(String token, String password) {
        String decodedToken;

        try {
            byte[] decodedBytes = Base64.getDecoder().decode(token);
            decodedToken = new String(decodedBytes, StandardCharsets.UTF_8);
        } catch (IllegalArgumentException e) {
            throw new BadRequestException("Token invalido");
        }

        UUID uuid;
        try {
            uuid = UUID.fromString(decodedToken);
        } catch (IllegalArgumentException e) {
            throw new BadRequestException("Token invalido");
        }

        Optional<PasswordResetToken> passwordResetTokenOpt = passwordResetTokenRepository.findById(uuid);
        if (passwordResetTokenOpt.isEmpty()) {
            throw new BadRequestException("Token invalido");
        }

        PasswordResetToken passwordResetToken = passwordResetTokenOpt.get();

        LocalDateTime now = LocalDateTime.now();
        if (passwordResetToken.getExpiresAt().isBefore(now)) {
            throw new BadRequestException("Token expirou");
        }

        Account account = passwordResetToken.getAccount();
        String encryptedPass = new BCryptPasswordEncoder().encode(password);
        account.setPassword(encryptedPass);
        accountRepository.save(account);

        passwordResetTokenRepository.delete(passwordResetToken);
    }
}
