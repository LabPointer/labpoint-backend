package com.backend.labpoint.repository;

import com.backend.labpoint.domain.users.Users;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

public interface UsersRepository extends JpaRepository<Users, UUID> {
    Optional<Users> findByRegistration(String registration);

    Optional<Users> findByEmail(String email);
}
