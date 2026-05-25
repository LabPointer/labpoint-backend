package com.backend.labpoint.repository;

import java.util.Optional;

import org.springframework.data.jpa.repository.JpaRepository;

import com.backend.labpoint.domain.user.UserSubject;

public interface UserSubjectRepository extends JpaRepository<UserSubject, Long> {
    Optional<UserSubject> findByFkUserIdAndFkSubjectId(Long userId, Long subjectId);
}
