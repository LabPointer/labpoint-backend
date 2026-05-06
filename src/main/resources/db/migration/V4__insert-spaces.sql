INSERT INTO spaces (name, capacity) VALUES ('informatica 1', 20);
INSERT INTO spaces (name, capacity) VALUES ('informatica 2', 20);
INSERT INTO spaces (name, capacity) VALUES ('informatica 8', 30);
INSERT INTO spaces (name, capacity) VALUES ('quimica 1', 50);
INSERT INTO spaces (name, capacity) VALUES ('auditorio', 150);

-- informatica 1 (id = 1)
INSERT INTO space_resources (resource, fk_space_id) VALUES ('COMPUTADORES', 1);
INSERT INTO space_resources (resource, fk_space_id) VALUES ('TELAO', 1);

-- informatica 2 (id = 2)
INSERT INTO space_resources (resource, fk_space_id) VALUES ('COMPUTADORES', 2);
INSERT INTO space_resources (resource, fk_space_id) VALUES ('TELAO', 2);

-- informatica 8 (id = 3)
INSERT INTO space_resources (resource, fk_space_id) VALUES ('COMPUTADORES', 3);
INSERT INTO space_resources (resource, fk_space_id) VALUES ('TELAO', 3);

-- quimica 1 (id = 4)
INSERT INTO space_resources (resource, fk_space_id) VALUES ('TUBOS_DE_ENSAIO', 4);

-- auditorio (id = 5)
INSERT INTO space_resources (resource, fk_space_id) VALUES ('TELAO', 5);