package com.backend.labpoint.service;

import java.util.List;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.data.domain.Pageable;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.jpa.domain.Specification;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import com.backend.labpoint.dto.user.ManageUserRequestDTO;
import com.backend.labpoint.dto.user.ManageUserResponseDTO;
import com.backend.labpoint.dto.user.ManageUserUpdateRequestDTO;
import com.backend.labpoint.entities.user.User;
import com.backend.labpoint.exception.ResourceNotFoundException;
import com.backend.labpoint.repository.UserRepository;
import com.backend.labpoint.specification.ManageUserSpecification;
import com.backend.labpoint.utils.UUIDExtractor;

@Service 
public class UserService {
    
    @Autowired
    private UserRepository userRepository;

    @Transactional(readOnly = true)
    public ResponseEntity<List<ManageUserResponseDTO>> getUsers(ManageUserRequestDTO params) {
        int pageSize = params.limit() > 0 ? params.limit() : 10;
        int pageNumber = params.offset() / pageSize;
        Pageable pageable = PageRequest.of(pageNumber, pageSize);

        Specification<User> spec = ManageUserSpecification.filters(params.username(), params.email(), params.registration(),
                params.role());

        List<User> users = userRepository.findAll(spec, pageable).getContent();
        List<ManageUserResponseDTO> userResponseDTOs = users.stream()
                .map(user -> new ManageUserResponseDTO(user.getUsername(), user.getEmail(), user.getRegistration(), user.getRole(), user.isEnabled(), user.getCreatedAt()))
                .toList();
        
        if (userResponseDTOs == null || userResponseDTOs.isEmpty())
            throw new ResourceNotFoundException("Nenhum usuario encontrado");

        return ResponseEntity.ok().body(userResponseDTOs);
    }

    @Transactional
    public ResponseEntity<Void> updateUserInfo(ManageUserUpdateRequestDTO data) {
        User user = userRepository.findByRegistration(data.registration())
                .orElseThrow(() -> new ResourceNotFoundException("Usuario nao encontrado"));

        if (data.username() != null && !data.username().isBlank()) {
            user.setUsername(data.username());
        }
        if (data.email() != null && !data.email().isBlank()) {
            user.setEmail(data.email());
        }
        if (data.registration() != null && !data.registration().isBlank()) {
            user.setRegistration(data.registration());
        }
        if (data.role() != null) {
            user.setRole(data.role());
        }
        if (data.password() != null && !data.password().isBlank()) {
            user.setPassword(data.password());
        }
        if (data.enabled() != null) {
            user.setEnabled(data.enabled());
        }
        if (data.enabled() != null) {
            user.setEnabled(data.enabled());
        }
        userRepository.save(user);

        return ResponseEntity.noContent().build();
    }
}
