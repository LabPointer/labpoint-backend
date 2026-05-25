package com.backend.labpoint.domain.space;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;
import org.hibernate.annotations.OnDelete;
import org.hibernate.annotations.OnDeleteAction;

import com.backend.labpoint.domain.resource.Resource;

@Entity
@Table(name = "space_resource")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class SpaceResource {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Integer id;

    @ManyToOne
    @OnDelete(action = OnDeleteAction.CASCADE)
    @JoinColumn(name = "fk_space_id", nullable = false)
    private Space space;

    @ManyToOne
    @OnDelete(action = OnDeleteAction.CASCADE)
    @JoinColumn(name = "fk_resource_id", nullable = false)
    private Resource resource;

    public SpaceResource(Space space, Resource resource) {
        this.space = space;
        this.resource = resource;
    }
}
