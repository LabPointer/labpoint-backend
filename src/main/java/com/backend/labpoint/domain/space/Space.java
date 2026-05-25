package com.backend.labpoint.domain.space;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Entity
@Table(name = "space")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class Space {
    private static final long serialVersionUID = 1L;

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Integer id;

    @Column(nullable = false, length = 32)
    private String name;

    @Column(length = 256)
    private String description;

    @Column(nullable = false)
    private int capacity;

    public Space(String name, String description, int capacity) {
        this.name = name;
        this.description = description;
        this.capacity = capacity;
    }

}
