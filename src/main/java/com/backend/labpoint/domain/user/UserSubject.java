package com.backend.labpoint.domain.user;

import com.backend.labpoint.domain.subject.Subject;

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
    private Integer id;

    @ManyToOne
    @JoinColumn(name = "fk_user_id", nullable = false)
    private User user;

    @ManyToOne
    @JoinColumn(name = "fk_subject_id", nullable = false)
    private Subject subject;
}
