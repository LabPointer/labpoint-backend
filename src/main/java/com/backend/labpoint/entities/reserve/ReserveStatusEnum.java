package com.backend.labpoint.entities.reserve;

public enum ReserveStatusEnum {
    CONFIRMED("CONFIRMED"),
    PENDING("PENDING"),
    ABSENT("ABSENT"),
    LOCKED("LOCKED"),
    CANCELED("CANCELED");
    private final String description;

    ReserveStatusEnum(String description) {
        this.description = description;
    }

    public String getDescription() {
        return description;
    }
}