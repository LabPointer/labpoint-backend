CREATE TABLE users(
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    username TEXT NOT NULL,
    email TEXT NOT NULL UNIQUE,
    registration TEXT NOT NULL UNIQUE,
    password TEXT NOT NULL,
    role TEXT NOT NULL
)