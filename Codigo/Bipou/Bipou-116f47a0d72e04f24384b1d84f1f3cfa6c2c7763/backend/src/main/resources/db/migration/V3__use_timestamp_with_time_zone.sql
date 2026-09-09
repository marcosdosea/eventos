ALTER TABLE registro
    ALTER COLUMN data_hora TYPE TIMESTAMP WITH TIME ZONE
    USING data_hora AT TIME ZONE 'UTC';

CREATE INDEX idx_registro_participante_data_hora
    ON registro (participante_id, data_hora DESC);
