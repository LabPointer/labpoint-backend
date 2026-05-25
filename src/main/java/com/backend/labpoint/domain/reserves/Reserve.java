package com.backend.labpoint.domain.reserves;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import org.hibernate.annotations.CreationTimestamp;
import org.hibernate.annotations.JdbcTypeCode;
import org.hibernate.annotations.OnDelete;
import org.hibernate.annotations.OnDeleteAction;
import org.hibernate.type.SqlTypes;

import com.backend.labpoint.domain.space.Space;
import com.backend.labpoint.domain.user.User;

import java.time.Instant;
import java.time.LocalDate;
import java.time.OffsetDateTime;

@Entity
@Table(name = "reserve")
@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
public class Reserve {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Integer id;

    @CreationTimestamp
    @Column(name = "created_at", nullable = false)
    private OffsetDateTime createdAt;

    @Column(name = "reserved_date", nullable = false)
    private LocalDate reservedDate;

    @Enumerated(EnumType.STRING)
    @JdbcTypeCode(SqlTypes.NAMED_ENUM)
    @Column(name = "schedule", nullable = false)
    private SchedulesEnum schedule;

    @ManyToOne
    @OnDelete(action = OnDeleteAction.CASCADE)
    @JoinColumn(name = "fk_user_id", nullable = false)
    private User user;

    @ManyToOne
    @OnDelete(action = OnDeleteAction.CASCADE)
    @JoinColumn(name = "fk_space_id", nullable = false)
    private Space space;

    public Reserve(LocalDate reservedDate, SchedulesEnum schedule, User user, Space space) {
        this.reservedDate = reservedDate;
        this.schedule = schedule;
        this.user = user;
        this.space = space;
    }
}

/*
@ElementCollection(targetClass = SchedulesEnum.class)
    @Enumerated(EnumType.STRING)
    @CollectionTable(name = "reserve_schedules", joinColumns = @JoinColumn(name = "reserve_id"))
    @Column(name = "schedule")
    private Set<SchedulesEnum> schedules;

    @ManyToOne
    @OnDelete(action = OnDeleteAction.CASCADE)
    @JoinColumn(name = "fk_space_id", nullable = false)
    private Spaces space;
*/
