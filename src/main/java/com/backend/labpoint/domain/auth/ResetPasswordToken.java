package com.backend.labpoint.domain.auth;

import com.backend.labpoint.domain.user.User;
import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.sql.Date;

@Entity
@Table(name = "reset_password_token")
@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
public class ResetPasswordToken {
    private static final int EXPIRATION = 20;

    @Id
    @GeneratedValue(strategy = GenerationType.AUTO)
    private Integer id;

    @Column(nullable = false)
    private String token;

    @Column(nullable = false)
    private Date expiryDate;

    @OneToOne(targetEntity = User.class, fetch = FetchType.EAGER)
    @JoinColumn(nullable = false, name = "fk_user_id")
    private User user;
}
