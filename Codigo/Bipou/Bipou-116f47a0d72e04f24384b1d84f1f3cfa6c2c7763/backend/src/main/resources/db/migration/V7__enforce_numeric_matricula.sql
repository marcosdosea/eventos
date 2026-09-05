WITH invalidas AS (
    SELECT id,
           row_number() OVER (ORDER BY id) AS ordem
    FROM participante
    WHERE matricula !~ '^[0-9]{12}$'
)
UPDATE participante p
SET matricula = '9' || lpad(invalidas.ordem::text, 11, '0')
FROM invalidas
WHERE p.id = invalidas.id;

ALTER TABLE participante
    ALTER COLUMN matricula TYPE VARCHAR(12);

ALTER TABLE participante
    ADD CONSTRAINT ck_participante_matricula_formato
        CHECK (
            char_length(matricula) = 12
            AND translate(matricula, '0123456789', '') = ''
        );
