package com.backend.labpoint.domain.resource;

import jakarta.persistence.*;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Entity
@Table(name = "resource")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class Resource {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    @NotNull
    private Integer id;

    @Column(nullable = false, length = 32, unique = true)
    @NotBlank
    private String name;

    public Resource(String name) {
        this.name = name;
    }
}
