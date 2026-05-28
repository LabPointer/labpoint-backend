package com.backend.labpoint.domain.space;

public enum ResourceEnum {
    TELAO("Telão"),
    COMPUTADORES("Computadores"),
    TUBOS_DE_ENSAIO("Tubos de Ensaio");

    private final String description;

    ResourceEnum(String description) {
        this.description = description;
    }

    public String getDescription() {
        return description;
    }
}
