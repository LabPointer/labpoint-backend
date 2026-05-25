package com.backend.labpoint.repository;

import org.springframework.data.jpa.repository.JpaRepository;

import com.backend.labpoint.domain.user.User;

import java.util.Optional;
import java.util.UUID;

public interface UsersRepository extends JpaRepository<User, UUID> {
    Optional<User> findByRegistration(String registration);

    Optional<User> findByEmail(String email);
}
