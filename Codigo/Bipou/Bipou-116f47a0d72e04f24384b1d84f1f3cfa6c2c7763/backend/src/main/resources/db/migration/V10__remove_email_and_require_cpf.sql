ALTER TABLE participante
    DROP COLUMN email,
    ALTER COLUMN cpf SET NOT NULL,
    ADD COLUMN cadastro_id UUID;

ALTER TABLE participante
    ADD CONSTRAINT participante_cadastro_id_key UNIQUE (cadastro_id);
