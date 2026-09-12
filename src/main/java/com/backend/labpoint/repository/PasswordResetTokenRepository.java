package com.backend.labpoint.repository;

import com.backend.labpoint.entities.reserve.Reserve;
import com.backend.labpoint.entities.resetpasswordtoken.PasswordResetToken;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;

public interface PasswordResetTokenRepository extends JpaRepository<PasswordResetToken, Integer>, JpaSpecificationExecutor<Reserve> {

}