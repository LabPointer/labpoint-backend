package com.backend.labpoint.service;

import com.backend.labpoint.dto.auth.SignUpRequestDTO;
import com.backend.labpoint.entities.account.Account;
import com.backend.labpoint.exception.BadRequestException;
import com.backend.labpoint.repository.AccountRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.domain.Specification;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

@Service
public class AuthService {

    @Autowired
    private AccountRepository userRepository;

    @Transactional(readOnly = true)
    public long countUsers(Specification<Account> spec) {
        return userRepository.count(spec);
    }

    @Transactional(readOnly = true)
    public List<Account> getUsers(Specification<Account> spec, Pageable pageable) {
        List<Account> users = userRepository.findAll(spec, pageable).getContent();
        if (users == null || users.isEmpty())
            throw new RuntimeException("Nenhum usuario encontrado");
        return users;
    }

    @Transactional
    public ResponseEntity<Object> registerNewUser(UserDetails userDetails, SignUpRequestDTO data) {
        if (userRepository.findByRegistration(data.registration()).isPresent()) {
            throw new BadRequestException("Usuario ja existe");
        }

        String encryptedPass = new BCryptPasswordEncoder().encode(data.password());
        Account user = new Account(data.username(), data.email(), data.registration(), encryptedPass, data.role());
        if (
                userDetails != null &&
                        userDetails.getAuthorities().stream().anyMatch(a -> a.getAuthority().equals("ROLE_ADMIN")) &&
                        data.enabled() != null
        ) {
            user.setEnabled(data.enabled());
        }

        userRepository.save(user);

        return ResponseEntity.status(HttpStatus.CREATED).build();
    }
}
