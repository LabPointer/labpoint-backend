package com.backend.labpoint.repository;

import java.util.Optional;

import org.springframework.data.jpa.repository.JpaRepository;

import com.backend.labpoint.domain.user.UserSubject;
import org.springframework.data.jpa.repository.Query;
import org.springframework.stereotype.Repository;

@Repository
public interface UserSubjectRepository extends JpaRepository<UserSubject, Long> {
    @Query("SELECT us FROM UserSubject us WHERE us.user.id = :userId")
    Optional<UserSubject> findByFkUserIdAndFkSubjectId(Long userId, Long subjectId);
}
