package com.backend.labpoint.entities.resource;

import com.backend.labpoint.entities.schedule.ReserveSchedule;
import jakarta.persistence.*;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
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
    @NotNull
    private Integer id;

    @Column(nullable = false, length = 32, unique = true)
    @NotBlank
    private String name;

    @OneToMany(mappedBy = "resource", fetch = FetchType.LAZY, cascade = CascadeType.ALL)
    private List<SpaceResource> spaces = new ArrayList<>();

    public Resource(String name) {
        this.name = name;
    }
}
