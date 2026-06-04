package com.backend.labpoint.service;

import com.backend.labpoint.domain.user.RegisterRequestDTO;
import com.backend.labpoint.domain.user.User;
import com.backend.labpoint.domain.user.UserUpdateRequestDTO;
import com.backend.labpoint.domain.user.UserUpdateResponseDTO;
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
    private AuthorizationService authorizationService;

    @Autowired
    private UserRepository usersRepository;

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
    public ResponseEntity<?> registerNewUser(UserDetails userDetails, RegisterRequestDTO data) {
        if (usersRepository.findByRegistration(data.registration()).isPresent()) {
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

        usersRepository.save(user);

        return ResponseEntity.status(HttpStatus.CREATED).build();
    }

    @Transactional
    public ResponseEntity<?> updateUserInfo(UserDetails userDetails, UserUpdateRequestDTO data) {
        User currentUser = usersRepository.findByRegistration(userDetails.getUsername()).orElseThrow();
        boolean isAdmin = userDetails.getAuthorities().stream().anyMatch(a -> a.getAuthority().equals("ROLE_ADMIN"));
        if (isAdmin) {
            if (data.uuid() != null) {
                if (currentUser.getId() == data.uuid())
                    throw new BadRequestException("id do corpo(body) da requisição não deve ser igual ao do usuario");

                if (!usersRepository.findByRegistration(data.registration()).isPresent())
                    throw new BadRequestException("Matricula ja registrada");

                var anotherUser = usersRepository.findById(data.uuid()).orElseThrow(() -> new ResourceNotFoundException("Usuario nao encontrado"));
                if (data.registration() != null && !data.registration().isBlank())
                    anotherUser.setRegistration(data.registration());
                if (data.username() != null && !data.username().isBlank())
                    anotherUser.setRegistration(data.registration());
                if (data.email() != null && !data.email().isBlank())
                    anotherUser.setEmail(data.email());
                if (data.password() != null && !data.password().isBlank())
                    anotherUser.setPassword(new BCryptPasswordEncoder().encode(data.password()));
                if (data.role() != null)
                    anotherUser.setRole(data.role());
                if (data.enabled() != null)
                    anotherUser.setEnabled(data.enabled());

                anotherUser = usersRepository.save(anotherUser);

                return ResponseEntity.ok(new UserUpdateResponseDTO(anotherUser.getId(), anotherUser.getRegistration(), anotherUser.getUsername(), anotherUser.getEmail(), anotherUser.getRole(), anotherUser.isEnabled()));
            } else {
                if (data.password() != null && !data.password().isBlank())
                    throw new BadRequestException("Usuario nao pode alterar a propria senha por essa rota");

                if (data.registration() != null && !data.registration().isBlank()) {
                    if (usersRepository.findByRegistration(data.registration()).isPresent())
                        throw new BadRequestException("Matricula ja registrada");
                    currentUser.setRegistration(data.registration());
                }
                if (data.email() != null && !data.email().isBlank())
                    currentUser.setEmail(data.email());
                if (data.username() != null && !data.username().isBlank())
                    currentUser.setUsername(data.username());
                if (data.role() != null)
                    currentUser.setRole(data.role());
                if (data.enabled() != null)
                    currentUser.setEnabled(data.enabled());

                currentUser = usersRepository.save(currentUser);

                return ResponseEntity.ok(new UserUpdateResponseDTO(null, currentUser.getRegistration(), currentUser.getUsername(), currentUser.getEmail(), currentUser.getRole(), null));
            }
        } else {
            if (data.uuid() != null)
                throw new ForbiddenException("Usuario precisa ser admin para editar contas de outros usuarios");

            if (data.registration() != null)
                throw new ForbiddenException("Usuario precisa ser admin para editar a matricula");

            if (data.enabled() != null)
                throw new ForbiddenException("Usuario precisa ser admin para habilitar ou desabilitar a conta");

            if (data.password() != null)
                throw new ForbiddenException("Usuario precisa ser admin e so pode alterar a senha caso seja de outro usuario");

            if (data.email() != null && !data.email().isBlank())
                currentUser.setEmail(data.email());
            if (data.username() != null && !data.username().isBlank())
                currentUser.setUsername(data.username());

            currentUser = usersRepository.save(currentUser);

            return ResponseEntity.ok(new UserUpdateResponseDTO(null, currentUser.getRegistration(), currentUser.getUsername(), currentUser.getEmail(), currentUser.getRole(), null));
        }
    }
}
