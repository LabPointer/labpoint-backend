package com.backend.labpoint.entities.schedule;

import com.backend.labpoint.entities.reserve.Reserve;
import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;
import org.hibernate.annotations.JdbcTypeCode;
import org.hibernate.annotations.OnDelete;
import org.hibernate.annotations.OnDeleteAction;
import org.hibernate.type.SqlTypes;

@Entity
@Table(name = "reserve_schedule")
@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
public class ReserveSchedule {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Integer id;

    @Enumerated(EnumType.STRING)
    @JdbcTypeCode(SqlTypes.NAMED_ENUM)
    @Column(name = "schedule", nullable = false)
    private SchedulesEnum schedule;

    @ManyToOne
    @OnDelete(action = OnDeleteAction.CASCADE)
    @JoinColumn(name = "fk_reserve_id", nullable = false)
    private Reserve reserve;

    public ReserveSchedule(SchedulesEnum schedule, Reserve reserve) {
        this.schedule = schedule;
        this.reserve = reserve;
    }
}
