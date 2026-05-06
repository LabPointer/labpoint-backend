CREATE TYPE schedules_enum AS ENUM (
    'M_AULA_1',
    'M_AULA_2',
    'M_AULA_3',
    'M_AULA_4',
    'M_AULA_5',
    'V_AULA_1',
    'V_AULA_2',
    'V_AULA_3',
    'V_AULA_4',
    'V_AULA_5',
    'N_AULA_1',
    'N_AULA_2',
    'N_AULA_3',
    'N_AULA_4'
);

CREATE TABLE reserves(
    id SERIAL PRIMARY KEY NOT NULL,
    reserved_date DATE NOT NULL,
    schedule schedules_enum NOT NULL,
    fk_space_id INT NOT NULL,
    FOREIGN KEY (fk_space_id) REFERENCES spaces(id) ON DELETE CASCADE,
    CONSTRAINT uq_space_schedule UNIQUE (fk_space_id, schedule)
);