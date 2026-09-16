package com.backend.labpoint.entities.password;

public enum PasswordEmailStatusEnum {
    SENT("SENT"),
    PENDING("PENDING");

    private final String description;

    PasswordEmailStatusEnum(String description) {
        this.description = description;
    }

    public String getDescription() {
        return description;
    }
}
