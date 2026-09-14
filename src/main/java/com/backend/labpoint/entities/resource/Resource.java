package com.backend.labpoint.entities.resource;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.util.ArrayList;
import java.util.List;

@Entity
@Table(name = "resource")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class Resource {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false, length = 64, unique = true)
    private String name;

    @OneToMany(mappedBy = "resource", fetch = FetchType.LAZY, cascade = CascadeType.ALL)
    private List<SpaceResource> spaces = new ArrayList<>();

    public Resource(String name) {
        this.name = name;
    }
}
