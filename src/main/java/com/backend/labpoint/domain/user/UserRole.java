package com.backend.labpoint.domain.user;

public enum UserRole {
    OWNER("owner"),
    ADMIN("admin"),
    USER("user");

    private final String role;

    UserRole(String role) {
        this.role = role;
    }

    public String getRole() {
        return role;
    }
}
