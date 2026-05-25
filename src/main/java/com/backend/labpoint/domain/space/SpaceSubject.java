package com.backend.labpoint.domain.space;

import com.backend.labpoint.domain.subject.Subject;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Entity
@Table(name = "space_subject")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class SpaceSubject {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Integer id;

    @ManyToOne
    @JoinColumn(name = "fk_space_id", nullable = false)
    private Space space;

    @ManyToOne
    @JoinColumn(name = "fk_subject_id", nullable = false)
    private Subject subject;

    public SpaceSubject(Space space, Subject subject) {
        this.space = space;
        this.subject = subject;
    }
}
