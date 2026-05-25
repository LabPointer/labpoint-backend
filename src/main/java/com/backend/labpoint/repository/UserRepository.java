package com.backend.labpoint.repository;

import org.springframework.data.jpa.repository.JpaRepository;

import com.backend.labpoint.domain.user.User;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;
import org.springframework.stereotype.Repository;

import java.util.Optional;
import java.util.UUID;

@Repository
public interface UserRepository extends JpaRepository<User, UUID>, JpaSpecificationExecutor<User> {
    Optional<User> findByRegistration(String registration);

    Optional<User> findByEmail(String email);
}
