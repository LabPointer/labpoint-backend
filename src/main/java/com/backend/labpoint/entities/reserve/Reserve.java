package com.backend.labpoint.entities.reserve;

import com.backend.labpoint.entities.account.Account;
import com.backend.labpoint.entities.schedule.ReserveSchedule;
import com.backend.labpoint.entities.space.Space;

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
    private Long id;

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
    private ReserveStatusEnum status = ReserveStatusEnum.CONFIRMED;

    @Column(name = "purpose", nullable = false)
    private String purpose;

    @ManyToOne
    @OnDelete(action = OnDeleteAction.CASCADE)
    @JoinColumn(name = "fk_user_id", nullable = false)
    private Account user;

    @ManyToOne
    @OnDelete(action = OnDeleteAction.CASCADE)
    @JoinColumn(name = "fk_space_id", nullable = false)
    private Space space;

    @OneToMany(mappedBy = "reserve", fetch = FetchType.LAZY, cascade = CascadeType.ALL)
    private List<ReserveSchedule> schedules = new ArrayList<>();

    public Reserve(LocalDate reservedDateFrom, LocalDate reservedDateTo, ReserveStatusEnum status, String purpose, Account user, Space space) {
        this.reservedDateFrom = reservedDateFrom;
        this.reservedDateTo = reservedDateTo;
        this.status = status;
        this.purpose = purpose;
        this.user = user;
        this.space = space;
    }
}
