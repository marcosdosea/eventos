ALTER TABLE participante
    ALTER COLUMN cpf TYPE VARCHAR(11);

ALTER TABLE participante
    ADD CONSTRAINT ck_participante_cpf_formato
        CHECK (
            char_length(cpf) = 11
            AND translate(cpf, '0123456789', '') = ''
        );
