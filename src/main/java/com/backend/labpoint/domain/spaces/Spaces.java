package com.backend.labpoint.domain.spaces;

import java.util.Set;

import lombok.Getter;
import lombok.Setter;

import jakarta.persistence.CollectionTable;
import jakarta.persistence.Column;
import jakarta.persistence.ElementCollection;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.Table;

import org.hibernate.annotations.JdbcTypeCode;
import org.hibernate.type.SqlTypes;

@Entity
@Table(name = "spaces")
@Getter
@Setter
public class Spaces {
    private static final long serialVersionUID = 1L;

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column( nullable = false, length = 32)
    private String name;

    @Column(nullable = false)
    private int capacity;

    @ElementCollection(targetClass = ResourcesEnum.class)
    @Enumerated(EnumType.STRING)
    @JdbcTypeCode(SqlTypes.NAMED_ENUM)
    @CollectionTable(name = "space_resources", joinColumns = @JoinColumn(name = "fk_space_id"))
    @Column(name = "resource")
    private Set<ResourcesEnum> resources;
 
}
