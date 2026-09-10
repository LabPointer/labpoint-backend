package com.backend.labpoint.entities.user;

import lombok.Getter;

@Getter
public enum UserRole {
    OWNER("owner"),
    ADMIN("admin"),
    USER("user");

    private final String role;

    UserRole(String role) {
        this.role = role;
    }

}
