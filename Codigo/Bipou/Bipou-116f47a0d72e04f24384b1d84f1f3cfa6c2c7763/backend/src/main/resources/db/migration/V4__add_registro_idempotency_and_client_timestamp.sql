ALTER TABLE registro
    ADD COLUMN leitura_id UUID;

UPDATE registro
SET leitura_id = gen_random_uuid()
WHERE leitura_id IS NULL;

ALTER TABLE registro
    ALTER COLUMN leitura_id SET NOT NULL,
    ADD CONSTRAINT uk_registro_leitura_id UNIQUE (leitura_id);

ALTER TABLE registro
    ADD COLUMN data_hora_lida_no_celular TIMESTAMP WITH TIME ZONE;

UPDATE registro
SET data_hora_lida_no_celular = data_hora
WHERE data_hora_lida_no_celular IS NULL;

ALTER TABLE registro
    ALTER COLUMN data_hora_lida_no_celular SET NOT NULL;
