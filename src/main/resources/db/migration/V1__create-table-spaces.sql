CREATE TABLE spaces(
    id SERIAL PRIMARY KEY NOT NULL,
    name varchar(32) UNIQUE NOT NULL,
    capacity INT NOT NULL
);