package com.backend.labpoint.repository;

import com.backend.labpoint.entities.password.PasswordResetToken;
import com.backend.labpoint.entities.reserve.Reserve;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;

import java.util.UUID;

public interface PasswordResetTokenRepository extends JpaRepository<PasswordResetToken, UUID>, JpaSpecificationExecutor<Reserve> {

}