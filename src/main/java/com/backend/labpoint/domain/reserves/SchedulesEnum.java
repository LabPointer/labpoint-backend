package com.backend.labpoint.domain.reserves;

public enum SchedulesEnum {
    M_AULA_1("M_AULA_1"),
    M_AULA_2("M_AULA_2"),
    M_AULA_3("M_AULA_3"),
    M_AULA_4("M_AULA_4"),
    M_AULA_5("M_AULA_5"),
    V_AULA_1("V_AULA_1"),
    V_AULA_2("V_AULA_2"),
    V_AULA_3("V_AULA_3"),
    V_AULA_4("V_AULA_4"),
    V_AULA_5("V_AULA_5"),
    N_AULA_1("N_AULA_1"),
    N_AULA_2("N_AULA_2"),
    N_AULA_3("N_AULA_3"),
    N_AULA_4("N_AULA_4");

    private final String description;

    SchedulesEnum(String description) {
        this.description = description;
    }

    public String getDescription() {
        return description;
    }
}
