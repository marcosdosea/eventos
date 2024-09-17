USE evento;
INSERT INTO `evento`.`TipoEvento` (`id`, `nome`) VALUES (1, 'Palestra');
INSERT INTO `evento`.`TipoEvento` (`id`, `nome`) VALUES (2, 'Mini-curso');
INSERT INTO `evento`.`TipoEvento` (`id`, `nome`) VALUES (3, 'Simpósio');
INSERT INTO `evento`.`TipoEvento` (`id`, `nome`) VALUES (4, 'Congresso');
INSERT INTO `evento`.`TipoEvento` (`id`, `nome`) VALUES (5, 'Workshop');
INSERT INTO `evento`.`TipoEvento` (`id`, `nome`) VALUES (6, 'Show');



INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('AC', 'Acre');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('AL', 'Alagoas');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('AP', 'Amapá');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('AM', 'Amazonas');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('BA', 'Bahia');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('CE', 'Ceará');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('ES', 'Espírito Santo');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('GO', 'Goiás');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('MA', 'Maranhão');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('MT', 'Mato Grosso');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('MS', 'Mato Grosso do Sul');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('MG', 'Minas Gerais');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('PA', 'Pará');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('PB', 'Paraíba');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('PR', 'Paraná');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('PE', 'Pernambuco');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('PI', 'Piauí');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('RJ', 'Rio de Janeiro');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('RN', 'Rio Grande do Norte');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('RS', 'Rio Grande do Sul');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('RO', 'Rondônia');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('RR', 'Roraima');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('SC', 'Santa Catarina');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('SP', 'São Paulo');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('SE', 'Sergipe');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('TO', 'Tocantins');
INSERT INTO `evento`.`EstadosBrasil` (`estado`, `nome`) VALUES ('DF', 'Distrito Federal');


INSERT INTO `evento`.`Papel` (`id`, `nome`) VALUES (1, 'Gestor do Sistema');
INSERT INTO `evento`.`Papel` (`id`, `nome`) VALUES (2, 'Gestor do Evento');
INSERT INTO `evento`.`Papel` (`id`, `nome`) VALUES (3, 'Colaborador');
INSERT INTO `evento`.`Papel` (`id`, `nome`) VALUES (4, 'Participante');

INSERT INTO `evento`.`AreaInteresse` (`id`, `nome`) VALUES (1, 'Show Musical');
INSERT INTO `evento`.`AreaInteresse` (`id`, `nome`) VALUES (2, 'Tecnologia');
INSERT INTO `evento`.`AreaInteresse` (`id`, `nome`) VALUES (3, 'Saúde');
INSERT INTO `evento`.`AreaInteresse` (`id`, `nome`) VALUES (4, 'Exposição Cultural');

INSERT INTO `evento`.`evento`
(`id`, `nome`, `descricao`, `dataInicio`, `dataFim`, `inscricaoGratuita`, `status`, `dataInicioInscricao`, `dataFimInscricao`, `valorInscricao`, `website`, `emailEvento`, `eventoPublico`, `cep`, `estado`,
`cidade`, `bairro`, `rua`, `numero`, `complemento`, `possuiCertificado`, `frequenciaMinimaCertificado`, `idTipoEvento`, `vagasOfertadas`, `vagasReservadas`, `vagasDisponiveis`, `tempoMinutosReserva`, `cargaHoraria`, `imagemPortal`) VALUES
(1, 'Congresso de Tecnologia', 'Um congresso focado em inovações tecnológicas.', '2024-10-01', '2024-10-05', 0, 'C', '2024-08-01', '2024-09-15', 100.00, 'http://congressotech.com', 'contact@congressotech.com', 1, '01001000', 'SP', 'São Paulo', 'Centro', 'Rua Exemplo', '100', 'Sala 10', 1, 75.00, 4, 500, 50, 450, 15, 40, 'imagem1.jpg'),
(2, 'Workshop de Saúde', 'Workshop sobre cuidados com a saúde.', '2024-11-01', '2024-11-03', 1, 'C', '2024-09-01', '2024-10-15', 0.00, 'http://workshopsaude.com', 'info@workshopsaude.com', 1, '40000000', 'BA', 'Salvador', 'Pituba', 'Avenida Saúde', '200', 'Bloco B', 0, 0.00, 5, 100, 10, 90, 15, 8, 'imagem2.jpg'),
(3, 'Show Musical', 'Um grande show com várias bandas.', '2024-12-20', '2024-12-21', 0, 'C', '2024-10-01', '2024-12-01', 50.00, 'http://showmusical.com', 'contato@showmusical.com', 1, '60000000', 'CE', 'Fortaleza', 'Meireles', 'Rua das Artes', '300', 'Apto 12', 1, 50.00, 6, 2000, 100, 1900, 15, 5, 'imagem3.jpg');


INSERT INTO `evento`.`subevento`
(`id`, `idEvento`, `nome`, `descricao`, `dataInicio`, `dataFim`, `inscricaoGratuita`, `status`, `dataInicioInscricao`, `dataFimInscricao`, `valorInscricao`,
`possuiCertificado`, `frequenciaMinimaCertificado`, `vagasOfertadas`, `vagasReservadas`, `vagasDisponiveis`, `cargaHoraria`, `idTipoEvento`) VALUES 
(1, 1, 'Palestra de Abertura', 'A palestra de abertura do congresso.', '2024-10-01 09:00:00', '2024-10-01 11:00:00', 0, 'C', '2024-08-01', '2024-09-15', 0.00, 1, 100.00, 200, 20, 180, 2, 1),
(2, 2, 'Oficina de Primeiros Socorros', 'Oficina prática de primeiros socorros.', '2024-11-02 14:00:00', '2024-11-02 17:00:00', 1, 'C', '2024-09-01', '2024-10-15', 0.00, 0, 0.00, 50, 5, 45, 3, 2),
(3, 3, 'Show de Encerramento', 'O show de encerramento com todas as bandas.', '2024-12-21 20:00:00', '2024-12-21 23:00:00', 0, 'C', '2024-10-01', '2024-12-01', 25.00, 1, 80.00, 1000, 50, 950, 3, 6);


INSERT INTO `evento`.`tipoinscricao`  (`id`, `idEvento`, `nome`, `descricao`, `valor`, `dataInicio`, `dataFim`, `usadaEvento`, `usadaSubevento`) VALUES 
(1, 1, 'Inscrição Padrão', 'Padrão', 50.00, '2024-08-01', '2024-09-15', 1, 0),
(2, 2, 'Inscrição Gratuita', 'Gratuito', 0.00, '2024-09-01', '2024-10-15', 1, 0),
(3, 3, 'Inscrição VIP', 'VIP', 100.00, '2024-10-01', '2024-12-01', 1, 1);


INSERT INTO `evento`.`modelocracha`  (`id`, `logotipo`, `texto`, `qrcode`, `idEvento`) VALUES 
(1, 'logotipo_evento1.png', 'Congresso de Tecnologia 2024', 1, 1),
(2, 'logotipo_evento2.png', 'Workshop de Saúde 2024', 1, 2),
(3, 'logotipo_evento3.png', 'Show Musical 2024', 0, 3);

use evento;

INSERT INTO `evento`.`pessoa` (`id`, `nome`, `nomeCracha`, `cpf`, `sexo`, `cep`, `rua`, `bairro`, `cidade`, `estado`, `numero`, `complemento`, `email`, `telefone1`, `telefone2`, `foto`) VALUES
(1, ' MARCOS BARBOSA DÓSEA', 'Dosea', '65535216038', 'M', '12345678', 'Rua A', 'Bairro X', 'São Paulo', 'SP', '100', 'Apto 101', 'dosea@gmail.com', '11987654321', '11912345678', 'foto1.jpg'),
(2, 'ICARO GABRIEL DO NASCIMENTO SANTOS', 'Ikaruz', '27382886000', 'F', '23456789', 'Rua B', 'Bairro Y', 'Rio de Janeiro', 'RJ', '200', 'Casa 02', 'ikaruz@gmail.com', '21998765432', '21923456789', 'foto2.jpg'),
(3, 'JOAO VITOR SODRE DE SOUSA', 'Sodre', '92023338077', 'M', '34567890', 'Rua C', 'Bairro Z', 'Belo Horizonte', 'MG', '300', 'Sala 03', 'sodre@gmail.com', '31987651234', '31912345678', 'foto3.jpg'),
(4, 'JORDAN SILVA SANTOS DE AQUINO ', 'Jordan', '40201971054', 'F', '45678901', 'Rua D', 'Bairro W', 'Curitiba', 'PR', '400', 'Apto 202', 'jordan@gmail.com', '41998761234', '41923456789', 'foto4.jpg'),
(5,'Valdir Mendonça Santana', 'Valdir','63499583062','M','45678901', 'Rua D', 'Bairro W', 'Curitiba', 'PR', '400', 'Apto 202','valdir@gmail.com','41998761234', '41923456789', 'foto5.jpg');

use itatechusers;

select * from aspnetroles;

INSERT INTO `aspnetusers` 
(`Id`, `UserName`, `NormalizedUserName`, `Email`, `NormalizedEmail`, `EmailConfirmed`, `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `PhoneNumber`, `PhoneNumberConfirmed`, `TwoFactorEnabled`, `LockoutEnd`, `LockoutEnabled`, `AccessFailedCount`)
VALUES
('187298b7-bb80-4824-a32f-c4698cea894b', '92023338077', '92023338077', 'sodre@gmail.com', 'SODRE@GMAIL.COM', '1', 'AQAAAAIAAYagAAAAENULGVGj30qK5lADm2Cdrt1ew4slalQsFMS3IEpf2K6dey9RZY12+0K/2/CHkWZDZA==', 'ZF7CKARJ2KMFF6VBGNEMBTRZUSE62ASZ', '0d9f7ba0-5991-42f4-95dc-c21f9296c69e', NULL, '0', '0', NULL, '1', '0'),
('58852b8c-53fb-4deb-921d-512e4e678838', '27382886000', '27382886000', 'ikaruz@gmail.com', 'IKARUZ@GMAIL.COM', '1', 'AQAAAAIAAYagAAAAELE3ZriXrXriQ6w5CPhyw9dBpFRKkMVG0EFlaAAH7w7Ht50FrHP/7DX2r5FYwlk1/w==', 'SFY2UP2J3XTBNROTB53VQCPHLGBP7Q2I', 'b2992759-c7de-4354-8f7f-05eb0915f933', NULL, '0', '0', NULL, '1', '0'),
('b3ee1ac9-e86d-46de-b322-ca532aa20fb0', '65535216038', '65535216038', 'dosea@gmail.com', 'DOSEA@GMAIL.COM', '1', 'AQAAAAIAAYagAAAAENnc/HNXjLRj5MA4EKh60MDuAJa7BqpZHKBtxQGLnERlyK61qVVv/vB1r/Fi0ZCcHw==', 'CEXQJ2BGDGLJKXPAPN3QQQ4KMNXCEDIL', '0221ec67-6da8-45b7-b6df-a13fb1e0be00', NULL, '0', '0', NULL, '1', '0'),
('c1c34ba6-646b-4a2d-85e7-ee9e14647a8c', '63499583062', '63499583062', 'valdir@gmail.com', 'VALDIR@GMAIL.COM', '1', 'AQAAAAIAAYagAAAAEBuIQMu3gmhzDkcnPV/Jv4+Ki/hlu9n8wK5Txz2v5v5OJPqKwRA7hNUYt5+GKMKQ1Q==', 'CNYTLD2HZYAAYXZ27MUWBZG5C7BO5YYF', '43bcb2ae-7ae5-4cc1-bca6-cb870668c54c', NULL, '0', '0', NULL, '1', '0'),
('eec1a3f4-2e74-45f2-b2b5-74273b8c144d', '40201971054', '40201971054', 'jordan@gmail.com', 'JORDAN@GMAIL.COM', '1', 'AQAAAAIAAYagAAAAEEwlZTPyVRX1gewZr/wr9HS0RHnVngaoKVmScO5I3IEtP7MW+Cg1B5cBrS/oaTYKrA==', 'LZEMCC4ZMLQQKM4LH2PI45JS6OXC6NYC', 'd904dc58-5ea3-4591-a886-e66eb1e76838', NULL, '0', '0', NULL, '1', '0');

INSERT INTO `aspnetuserroles` (`UserId`, `RoleId`) VALUES
('b3ee1ac9-e86d-46de-b322-ca532aa20fb0', '1'), -- Dosea com papel ADMINISTRADOR
('58852b8c-53fb-4deb-921d-512e4e678838', '1'), -- Icaro com papel ADMINISTRADOR
('187298b7-bb80-4824-a32f-c4698cea894b', '2'), -- Sodre com papel GESTOR
('c1c34ba6-646b-4a2d-85e7-ee9e14647a8c', '3'), -- Valdir com papel COLABORADOR
('eec1a3f4-2e74-45f2-b2b5-74273b8c144d','4'); -- Jordan com papel Usuário
use evento;
select * from pessoa;
use itatechusers;
select * from aspnetusers;
select * from aspnetuserroles;



-- MESMA SENHA PARA TODOS OS USUÁRIOS - SENHA: 123456



