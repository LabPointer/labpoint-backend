package com.backend.labpoint.domain.reserve;

import com.backend.labpoint.domain.space.Space;
import com.backend.labpoint.domain.user.User;
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

import java.time.LocalDate;
import java.time.OffsetDateTime;
import java.util.ArrayList;
import java.util.List;

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

    @Column(name = "reserved_date_from", nullable = false)
    private LocalDate reservedDateFrom;

    @Column(name = "reserved_date_to", nullable = false)
    private LocalDate reservedDateTo;

    @Enumerated(EnumType.STRING)
    @JdbcTypeCode(SqlTypes.NAMED_ENUM)
    @Column(name = "status", nullable = false)
    private ScheduleStatusEnum status = ScheduleStatusEnum.CONFIRMED;

    @Column(name = "purpose", nullable = false)
    private String purpose;

    @ManyToOne
    @OnDelete(action = OnDeleteAction.CASCADE)
    @JoinColumn(name = "fk_user_id", nullable = false)
    private User user;

    @ManyToOne
    @OnDelete(action = OnDeleteAction.CASCADE)
    @JoinColumn(name = "fk_space_id", nullable = false)
    private Space space;

    @OneToMany(mappedBy = "reserve", fetch = FetchType.LAZY, cascade = CascadeType.ALL)
    private List<ReserveSchedule> schedules = new ArrayList<>();

    public Reserve(LocalDate reservedDateFrom, LocalDate reservedDateTo, ScheduleStatusEnum status, String purpose, User user, Space space) {
        this.reservedDateFrom = reservedDateFrom;
        this.reservedDateTo = reservedDateTo;
        this.status = status;
        this.purpose = purpose;
        this.user = user;
        this.space = space;
    }
}
