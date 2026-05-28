INSERT INTO space (name, capacity, description) VALUES ('informatica 2', 20, 'Laboratorio de informatica');
INSERT INTO space (name, capacity, description) VALUES ('informatica 8', 30, 'Laboratorio de informatica');
INSERT INTO space (name, capacity, description) VALUES ('quimica analitica', 50, 'Laboratorio de quimica analitica');
INSERT INTO space (name, capacity, description) VALUES ('auditorio', 150, 'Auditorio para palestras');

-- Turmas
INSERT INTO subject (name) VALUES ('ADS');
INSERT INTO subject (name) VALUES ('quimica analitica');

-- recursos
INSERT INTO resource (name) VALUES ('notebooks');
INSERT INTO resource (name) VALUES ('televisao');
INSERT INTO resource (name) VALUES ('telao');
INSERT INTO resource (name) VALUES ('tubos de ensaio');

-- informatica 2 (id = 1)
INSERT INTO space_resource (fk_space_id, fk_resource_id) VALUES (1, 1);
INSERT INTO space_resource (fk_space_id, fk_resource_id) VALUES (1, 2);
INSERT INTO space_subject (fk_space_id, fk_subject_id) VALUES (1, 1);

-- informatica 8 (id = 2)
INSERT INTO space_resource (fk_space_id, fk_resource_id) VALUES (2, 1);
INSERT INTO space_resource (fk_space_id, fk_resource_id) VALUES (2, 3);
INSERT INTO space_subject (fk_space_id, fk_subject_id) VALUES (2, 1);

-- quimica analitica (id = 3)
INSERT INTO space_resource (fk_space_id, fk_resource_id) VALUES (3, 4);
INSERT INTO space_subject (fk_space_id, fk_subject_id) VALUES (3, 2);