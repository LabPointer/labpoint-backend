package com.backend.labpoint.service;

import com.backend.labpoint.domain.user.User;
import com.backend.labpoint.domain.user.UserUpdateRequestDTO;
import com.backend.labpoint.repository.UserRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.data.jpa.domain.Specification;
import org.springframework.stereotype.Service;
import org.springframework.data.domain.Pageable;
import java.util.List;

@Service
public class AuthService {

    @Autowired
    private AuthorizationService authorizationService;

    @Autowired
    private UserRepository userRepository;

    public long countUsers(Specification<User> spec) {
        return  userRepository.count(spec);
    }

    public List<User> getUsers(Specification<User> spec, Pageable pageable) {
        return userRepository.findAll(spec, pageable).getContent();
    }
}
