package com.backend.labpoint.domain.reserve;

public enum ScheduleStatusEnum {
    CONFIRMED("CONFIRMED"),
    PENDING("PNDING"),
    LOCKED("LOCKED"),
    CANCELED("CANCELED");
    private final String description;

    ScheduleStatusEnum(String description) {
        this.description = description;
    }

    public String getDescription() {
        return description;
    }
}