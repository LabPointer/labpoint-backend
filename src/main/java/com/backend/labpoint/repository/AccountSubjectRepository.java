package com.backend.labpoint.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.stereotype.Repository;

import com.backend.labpoint.entities.subject.AccountSubject;

import java.util.Optional;

@Repository
public interface AccountSubjectRepository extends JpaRepository<AccountSubject, Long> {
    @Query("SELECT us FROM AccountSubject us WHERE us.account.id = :userId")
    Optional<AccountSubject> findByFkUserIdAndFkSubjectId(Long userId, Long subjectId);
}
