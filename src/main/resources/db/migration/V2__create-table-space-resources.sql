/*
CREATE TYPE resources_enum AS ENUM (
    'TELAO',
    'COMPUTADORES',
    'TUBOS_DE_ENSAIO'
);
*/

CREATE TABLE space_resources(
    id SERIAL PRIMARY KEY NOT NULL,
    name varchar(32) NOT NULL,
    fk_space_id INT NOT NULL,
    FOREIGN KEY (fk_space_id) REFERENCES spaces(id),
    CONSTRAINT uq_space_resource UNIQUE (fk_space_id, name)
);