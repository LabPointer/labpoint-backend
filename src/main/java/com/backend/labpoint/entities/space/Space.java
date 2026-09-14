package com.backend.labpoint.entities.space;

import com.backend.labpoint.entities.reserve.Reserve;
import com.backend.labpoint.entities.resource.SpaceResource;
import com.backend.labpoint.entities.subject.SpaceSubject;
import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.util.ArrayList;
import java.util.List;

@Entity
@Table(name = "space")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class Space {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false, length = 64, unique = true)
    private String name;

    @Column(length = 256)
    private String description;

    @Column(nullable = false)
    private int capacity;

    @Column(nullable = false)
    private boolean locked;

    @OneToMany(mappedBy = "space", fetch = FetchType.LAZY, cascade = CascadeType.ALL)
    private List<SpaceResource> resources = new ArrayList<>();

    @OneToMany(mappedBy = "space", fetch = FetchType.LAZY, cascade = CascadeType.ALL)
    private List<SpaceSubject> subjects = new ArrayList<>();

    @OneToMany(mappedBy = "space", fetch = FetchType.LAZY, cascade = CascadeType.ALL)
    private List<Reserve> reserves = new ArrayList<>();

    public Space(String name, String description, int capacity, boolean locked) {
        this.name = name;
        this.description = description;
        this.capacity = capacity;
        this.locked = locked;
    }

}
