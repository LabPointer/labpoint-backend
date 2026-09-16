package com.backend.labpoint.repository;

import com.backend.labpoint.entities.password.PasswordResetToken;
import com.backend.labpoint.entities.reserve.Reserve;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;
import org.springframework.data.jpa.repository.Query;

import java.util.List;
import java.util.UUID;

public interface PasswordResetTokenRepository extends JpaRepository<PasswordResetToken, UUID>, JpaSpecificationExecutor<Reserve> {
    @Query ("SELECT p FROM PasswordResetToken p WHERE p.status = 'PENDING' LIMIT 50")
    List<PasswordResetToken> findByPendingStatus();
}