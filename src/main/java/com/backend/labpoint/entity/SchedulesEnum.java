package com.backend.labpoint.entity;

public enum SchedulesEnum {
    M_AULA_1("M-Aula1"),
    M_AULA_2("M-Aula2"),
    M_AULA_3("M-Aula3"),
    M_AULA_4("M-Aula4"),
    M_AULA_5("M-Aula5"),
    V_AULA_1("V-Aula1"),
    V_AULA_2("V-Aula2"),
    V_AULA_3("V-Aula3"),
    V_AULA_4("V-Aula4"),
    V_AULA_5("V-Aula5"),
    N_AULA_1("N-Aula1"),
    N_AULA_2("N-Aula2"),
    N_AULA_3("N-Aula3"),
    N_AULA_4("N-Aula4");

    private final String description;

    SchedulesEnum(String description) {
        this.description = description;
    }

    public String getDescription() {
        return description;
    }
}
