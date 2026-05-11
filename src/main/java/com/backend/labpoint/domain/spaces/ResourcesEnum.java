package com.backend.labpoint.domain.spaces;

public enum ResourcesEnum {
    TELAO("Telão"),
    COMPUTADORES("Computadores"),
    TUBOS_DE_ENSAIO("Tubos de Ensaio");

    private final String description;

    ResourcesEnum(String description) {
        this.description = description;
    }

    public String getDescription() {
        return description;
    }
}
