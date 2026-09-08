UPDATE registro
SET tipo_acao = CASE tipo_acao
    WHEN 'SAIDA_BREAK' THEN 'SAIDA'
    WHEN 'RETORNO_BREAK' THEN 'ENTRADA'
    ELSE tipo_acao
END
WHERE tipo_acao IN ('SAIDA_BREAK', 'RETORNO_BREAK');

ALTER TABLE registro
    ADD CONSTRAINT ck_registro_tipo_acao
        CHECK (tipo_acao IN ('ENTRADA', 'SAIDA'));

ALTER TABLE participante
    DROP CONSTRAINT IF EXISTS uk_participante_matricula,
    DROP CONSTRAINT IF EXISTS ck_participante_matricula_nao_vazia,
    DROP CONSTRAINT IF EXISTS ck_participante_matricula_formato,
    DROP COLUMN matricula;
