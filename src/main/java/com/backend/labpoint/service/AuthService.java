package com.backend.labpoint.service;

import com.backend.labpoint.entities.user.User;
import com.backend.labpoint.dto.auth.RegisterRequestDTO;
import com.backend.labpoint.dto.user.UserUpdateRequestDTO;
import com.backend.labpoint.dto.user.UserUpdateResponseDTO;
import com.backend.labpoint.exception.BadRequestException;
import com.backend.labpoint.exception.ForbiddenException;
import com.backend.labpoint.exception.ResourceNotFoundException;
import com.backend.labpoint.repository.UserRepository;
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
    private UserRepository userRepository;

    @Transactional(readOnly = true)
    public long countUsers(Specification<User> spec) {
        return userRepository.count(spec);
    }

    @Transactional(readOnly = true)
    public List<User> getUsers(Specification<User> spec, Pageable pageable) {
        List<User> users = userRepository.findAll(spec, pageable).getContent();
        if (users == null || users.isEmpty())
            throw new RuntimeException("Nenhum usuario encontrado");
        return users;
    }

    @Transactional
    public ResponseEntity<Object> registerNewUser(UserDetails userDetails, RegisterRequestDTO data) {
        if (userRepository.findByRegistration(data.registration()).isPresent()) {
            throw new BadRequestException("Usuario ja existe");
        }

        String encryptedPass = new BCryptPasswordEncoder().encode(data.password());
        User user = new User(data.username(), data.email(), data.registration(), encryptedPass, data.role());
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
