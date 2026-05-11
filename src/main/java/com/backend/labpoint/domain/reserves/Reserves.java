package com.backend.labpoint.domain.reserves;

import java.time.LocalDate;

import com.backend.labpoint.domain.spaces.Spaces;
import lombok.Getter;
import lombok.Setter;
import org.hibernate.annotations.OnDelete;
import org.hibernate.annotations.OnDeleteAction;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.Table;

import org.hibernate.annotations.JdbcTypeCode;
import org.hibernate.type.SqlTypes;

@Entity
@Table(name = "reserves")
@Getter
@Setter
public class Reserves {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "reserved_date", nullable = false)
    private LocalDate reservedDate;

    @Enumerated(EnumType.STRING)
    @JdbcTypeCode(SqlTypes.NAMED_ENUM)
    @Column(name = "schedule", nullable = false)
    private SchedulesEnum schedule;

    @ManyToOne
    @OnDelete(action = OnDeleteAction.CASCADE)
    @JoinColumn(name = "fk_space_id", nullable = false)
    private Spaces space;
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
