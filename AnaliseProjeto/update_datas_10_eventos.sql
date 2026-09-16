-- Script para atualizar as datas de 10 eventos (IDs 2 a 11) para datas mais recentes (Setembro/Outubro de 2026)
-- Rodar este script diretamente no banco de dados MySQL para que os eventos fiquem com status condizentes com o período atual (16/09/2026).

USE `evento`;

-- Evento ID 2: Workshop de Saúde
UPDATE `Evento` 
SET `dataInicioInscricao` = '2026-09-01 00:00:00', `dataFimInscricao` = '2026-10-01 00:00:00', `dataInicio` = '2026-10-05 08:00:00', `dataFim` = '2026-10-06 18:00:00' 
WHERE `id` = 2;

-- Evento ID 3: Show Musical
UPDATE `Evento` 
SET `dataInicioInscricao` = '2026-09-10 00:00:00', `dataFimInscricao` = '2026-09-20 00:00:00', `dataInicio` = '2026-09-25 19:00:00', `dataFim` = '2026-09-25 23:00:00' 
WHERE `id` = 3;

-- Evento ID 4: Show de Rock Clássico
UPDATE `Evento` 
SET `dataInicioInscricao` = '2026-09-15 00:00:00', `dataFimInscricao` = '2026-09-30 00:00:00', `dataInicio` = '2026-10-02 20:00:00', `dataFim` = '2026-10-03 01:00:00' 
WHERE `id` = 4;

-- Evento ID 5: Festival de Jazz e Blues (Inscrições encerradas recentemente)
UPDATE `Evento` 
SET `dataInicioInscricao` = '2026-08-01 00:00:00', `dataFimInscricao` = '2026-09-15 00:00:00', `dataInicio` = '2026-09-18 17:00:00', `dataFim` = '2026-09-20 22:00:00' 
WHERE `id` = 5;

-- Evento ID 6: Eletrônica Sunset
UPDATE `Evento` 
SET `dataInicioInscricao` = '2026-09-01 00:00:00', `dataFimInscricao` = '2026-10-10 00:00:00', `dataInicio` = '2026-10-15 15:00:00', `dataFim` = '2026-10-16 02:00:00' 
WHERE `id` = 6;

-- Evento ID 7: Noite de MPB (Inscrições abrem hoje)
UPDATE `Evento` 
SET `dataInicioInscricao` = '2026-09-16 00:00:00', `dataFimInscricao` = '2026-10-05 00:00:00', `dataInicio` = '2026-10-10 21:00:00', `dataFim` = '2026-10-11 02:00:00' 
WHERE `id` = 7;

-- Evento ID 8: Pagode na Praça (Inscrições abrem no futuro)
UPDATE `Evento` 
SET `dataInicioInscricao` = '2026-09-20 00:00:00', `dataFimInscricao` = '2026-10-15 00:00:00', `dataInicio` = '2026-10-20 14:00:00', `dataFim` = '2026-10-20 20:00:00' 
WHERE `id` = 8;

-- Evento ID 9: Palestra: IA no Futuro do Trabalho
UPDATE `Evento` 
SET `dataInicioInscricao` = '2026-09-05 00:00:00', `dataFimInscricao` = '2026-09-25 00:00:00', `dataInicio` = '2026-09-30 09:00:00', `dataFim` = '2026-09-30 12:00:00' 
WHERE `id` = 9;

-- Evento ID 10: Palestra: Sustentabilidade Empresarial (Inscrições encerram hoje)
UPDATE `Evento` 
SET `dataInicioInscricao` = '2026-09-01 00:00:00', `dataFimInscricao` = '2026-09-16 23:59:59', `dataInicio` = '2026-09-22 14:00:00', `dataFim` = '2026-09-22 17:00:00' 
WHERE `id` = 10;

-- Evento ID 11: Palestra: Saúde Mental na Era Digital
UPDATE `Evento` 
SET `dataInicioInscricao` = '2026-09-15 00:00:00', `dataFimInscricao` = '2026-10-10 00:00:00', `dataInicio` = '2026-10-15 10:00:00', `dataFim` = '2026-10-15 13:00:00' 
WHERE `id` = 11;
