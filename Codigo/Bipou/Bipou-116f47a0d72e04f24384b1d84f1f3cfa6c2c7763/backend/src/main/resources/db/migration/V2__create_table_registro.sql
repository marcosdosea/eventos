CREATE TABLE registro (
    id UUID PRIMARY KEY,
    participante_id UUID NOT NULL,
    tipo_acao VARCHAR(30) NOT NULL,
    data_hora TIMESTAMP NOT NULL,
    dispositivo_id VARCHAR(100) NOT NULL,
    CONSTRAINT fk_registro_participante
        FOREIGN KEY (participante_id)
        REFERENCES participante (id)
);
