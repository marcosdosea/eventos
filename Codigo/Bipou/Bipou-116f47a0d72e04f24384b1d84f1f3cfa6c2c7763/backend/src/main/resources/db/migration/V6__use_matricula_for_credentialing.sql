UPDATE participante
SET matricula = 'LEGACY-' || substring(replace(id::text, '-', '') from 1 for 12)
WHERE matricula IS NULL OR btrim(matricula) = '';

WITH duplicadas AS (
    SELECT id,
           row_number() OVER (PARTITION BY matricula ORDER BY id) AS ordem
    FROM participante
)
UPDATE participante p
SET matricula = p.matricula || '-' || substring(replace(p.id::text, '-', '') from 1 for 8)
FROM duplicadas d
WHERE p.id = d.id
  AND d.ordem > 1;

ALTER TABLE participante
    ALTER COLUMN cpf DROP NOT NULL;

ALTER TABLE participante
    ALTER COLUMN matricula SET NOT NULL;

ALTER TABLE participante
    ADD CONSTRAINT uk_participante_matricula UNIQUE (matricula);

ALTER TABLE participante
    ADD CONSTRAINT ck_participante_matricula_nao_vazia
        CHECK (btrim(matricula) <> '');
