package com.backend.labpoint.entities.subject;

import com.backend.labpoint.entities.account.Account;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Entity
@Table(name = "user_subject")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class UserSubject {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne
    @JoinColumn(name = "fk_user_id", nullable = false)
    private Account user;

    @ManyToOne
    @JoinColumn(name = "fk_subject_id", nullable = false)
    private Subject subject;
}
