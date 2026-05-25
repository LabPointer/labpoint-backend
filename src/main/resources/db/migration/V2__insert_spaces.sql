INSERT INTO spaces (name, capacity) VALUES ('informatica 1', 20);
INSERT INTO spaces (name, capacity) VALUES ('informatica 2', 20);
INSERT INTO spaces (name, capacity) VALUES ('informatica 8', 30);
INSERT INTO spaces (name, capacity) VALUES ('quimica 1', 50);
INSERT INTO spaces (name, capacity) VALUES ('auditorio', 150);

-- informatica 1 (id = 1)
INSERT INTO space_resources (name, fk_space_id) VALUES ('computadores', 1);
INSERT INTO space_resources (name, fk_space_id) VALUES ('telao', 1);

-- informatica 2 (id = 2)
INSERT INTO space_resources (name, fk_space_id) VALUES ('computadores', 2);
INSERT INTO space_resources (name, fk_space_id) VALUES ('telao', 2);

-- informatica 8 (id = 3)
INSERT INTO space_resources (name, fk_space_id) VALUES ('computadores', 3);
INSERT INTO space_resources (name, fk_space_id) VALUES ('telao', 3);

-- quimica 1 (id = 4)
INSERT INTO space_resources (name, fk_space_id) VALUES ('tubos de ensaio', 4);

-- auditorio (id = 5)
INSERT INTO space_resources (name, fk_space_id) VALUES ('telao', 5);