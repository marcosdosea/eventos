ALTER TABLE participante
    ADD COLUMN id_pessoa_borala BIGINT,
    ADD COLUMN evento_id BIGINT;

UPDATE participante
SET evento_id = 0
WHERE evento_id IS NULL;

ALTER TABLE participante
    ALTER COLUMN evento_id SET NOT NULL;

ALTER TABLE participante
    DROP CONSTRAINT IF EXISTS participante_cpf_key,
    ADD CONSTRAINT uk_participante_evento_cpf UNIQUE (evento_id, cpf);

CREATE INDEX idx_participante_evento_id
    ON participante (evento_id);

CREATE INDEX idx_participante_id_pessoa_borala
    ON participante (id_pessoa_borala);

CREATE UNIQUE INDEX uk_participante_evento_pessoa_borala
    ON participante (evento_id, id_pessoa_borala)
    WHERE id_pessoa_borala IS NOT NULL;

CREATE TABLE participante_subevento (
    participante_id UUID NOT NULL,
    subevento_id BIGINT NOT NULL,
    CONSTRAINT pk_participante_subevento PRIMARY KEY (participante_id, subevento_id),
    CONSTRAINT fk_participante_subevento_participante
        FOREIGN KEY (participante_id)
        REFERENCES participante (id)
        ON DELETE CASCADE
);

CREATE INDEX idx_participante_subevento_subevento_id
    ON participante_subevento (subevento_id);

ALTER TABLE registro
    ADD COLUMN evento_id BIGINT,
    ADD COLUMN subevento_id BIGINT,
    ADD COLUMN operador_usuario VARCHAR(100);

UPDATE registro r
SET evento_id = COALESCE(
    (SELECT p.evento_id FROM participante p WHERE p.id = r.participante_id),
    0
)
WHERE r.evento_id IS NULL;

ALTER TABLE registro
    ALTER COLUMN evento_id SET NOT NULL;

CREATE INDEX idx_registro_contexto_participante_data_hora
    ON registro (participante_id, evento_id, subevento_id, data_hora DESC);

CREATE INDEX idx_registro_evento_subevento_data_hora
    ON registro (evento_id, subevento_id, data_hora DESC);
