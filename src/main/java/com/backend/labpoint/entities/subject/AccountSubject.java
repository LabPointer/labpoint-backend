package com.backend.labpoint.entities.subject;

import com.backend.labpoint.entities.account.Account;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Entity
@Table(name = "account_subject")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class AccountSubject {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne
    @JoinColumn(name = "fk_account_id", nullable = false)
    private Account account;

    @ManyToOne
    @JoinColumn(name = "fk_subject_id", nullable = false)
    private Subject subject;
}
