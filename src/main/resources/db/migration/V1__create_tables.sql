CREATE TABLE "user" (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    username TEXT NOT NULL,
    email TEXT NOT NULL,
    registration TEXT NOT NULL UNIQUE,
    password TEXT NOT NULL,
    role TEXT NOT NULL,
    enabled BOOLEAN DEFAULT false
);

CREATE TABLE subject(
    id SERIAL PRIMARY KEY NOT NULL,
    name varchar(32) NOT NULL UNIQUE
);

CREATE TABLE user_subject(
    id SERIAL PRIMARY KEY NOT NULL,
    fk_user_id UUID NOT NULL,
    fk_subject_id INT NOT NULL,
    FOREIGN KEY (fk_user_id) REFERENCES "user"(id) ON DELETE CASCADE,
    FOREIGN KEY (fk_subject_id) REFERENCES subject(id) ON DELETE CASCADE,
    CONSTRAINT uq_user_subject UNIQUE (fk_user_id, fk_subject_id)
);

CREATE TABLE space(
    id SERIAL PRIMARY KEY NOT NULL,
    name varchar(32) UNIQUE NOT NULL,
    description varchar(256),
    capacity INT NOT NULL
);

CREATE TABLE space_subject(
    id SERIAL PRIMARY KEY NOT NULL,
    fk_space_id INT NOT NULL,
    fk_subject_id INT NOT NULL,
    FOREIGN KEY (fk_space_id) REFERENCES space(id) ON DELETE CASCADE,
    FOREIGN KEY (fk_subject_id) REFERENCES subject(id) ON DELETE CASCADE,
    CONSTRAINT uq_space_subject UNIQUE (fk_space_id, fk_subject_id)
);

CREATE TABLE resource(
    id SERIAL PRIMARY KEY NOT NULL,
    name varchar(32) NOT NULL UNIQUE
);

CREATE TABLE space_resource(
    id SERIAL PRIMARY KEY NOT NULL,
    fk_space_id INT NOT NULL,
    fk_resource_id INT NOT NULL,
    FOREIGN KEY (fk_space_id) REFERENCES space(id),
    FOREIGN KEY (fk_resource_id) REFERENCES resource(id),
    CONSTRAINT uq_space_resource UNIQUE (fk_space_id, fk_resource_id)
);

CREATE TYPE schedule_enum AS ENUM (
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

CREATE TABLE reserve(
    id SERIAL PRIMARY KEY NOT NULL,
    created_at timestamptz DEFAULT NOW(),
    reserved_date DATE NOT NULL,
    schedule schedule_enum NOT NULL,
    fk_user_id UUID NOT NULL,
    fk_space_id INT NOT NULL,
    FOREIGN KEY (fk_user_id) REFERENCES "user"(id) ON DELETE CASCADE,
    FOREIGN KEY (fk_space_id) REFERENCES space(id) ON DELETE CASCADE,
    CONSTRAINT uq_space_schedule UNIQUE (fk_space_id, schedule)
);

