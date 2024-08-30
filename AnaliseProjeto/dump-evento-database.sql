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

INSERT INTO `evento`.`pessoa` (`id`, `nome`, `nomeCracha`, `cpf`, `sexo`, `cep`, `rua`, `bairro`, `cidade`, `estado`, `numero`, `complemento`, `email`, `telefone1`, `telefone2`, `foto`) VALUES
(1, 'João Silva', 'JoãoS', '65535216038', 'M', '12345678', 'Rua A', 'Bairro X', 'São Paulo', 'SP', '100', 'Apto 101', 'joao.silva@example.com', '11987654321', '11912345678', 'foto1.jpg'),
(2, 'Maria Oliveira', 'MariaO', '27382886000', 'F', '23456789', 'Rua B', 'Bairro Y', 'Rio de Janeiro', 'RJ', '200', 'Casa 02', 'maria.oliveira@example.com', '21998765432', '21923456789', 'foto2.jpg'),
(3, 'Pedro Santos', 'PedroS', '92023338077', 'M', '34567890', 'Rua C', 'Bairro Z', 'Belo Horizonte', 'MG', '300', 'Sala 03', 'pedro.santos@example.com', '31987651234', '31912345678', 'foto3.jpg'),
(4, 'Ana Costa', 'AnaC', '40201971054', 'F', '45678901', 'Rua D', 'Bairro W', 'Curitiba', 'PR', '400', 'Apto 202', 'ana.costa@example.com', '41998761234', '41923456789', 'foto4.jpg'),
(5,'Valdir Mendonça Santana', 'V-San','63499583062','M','45678901', 'Rua D', 'Bairro W', 'Curitiba', 'PR', '400', 'Apto 202','valdirsantana53@gmail.com','41998761234', '41923456789', 'foto5.jpg');

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

INSERT INTO `evento`.`inscricaopessoaevento` (`id`, `idPessoa`, `idEventoinscricaopessoaevento`, `idPapel`, `idTipoInscricao`, `dataInscricao`, `valorTotal`, `status`, `frequenciaFinal`, `nomeCracha`)
VALUES
(<{id: }>,
<{idPessoa: }>,
<{idEvento: }>,
<{idPapel: }>,
<{idTipoInscricao: }>,
<{dataInscricao: }>,
<{valorTotal: 0.00}>,
<{status: S}>,
<{frequenciaFinal: 0.00}>,
<{nomeCracha: }>);


use itatechusers;

INSERT INTO `aspnetusers` 
(`Id`, `UserName`, `NormalizedUserName`, `Email`, `NormalizedEmail`, `EmailConfirmed`, `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `PhoneNumber`, `PhoneNumberConfirmed`, `TwoFactorEnabled`, `LockoutEnd`, `LockoutEnabled`, `AccessFailedCount`)
VALUES
('1', '65535216038', '65535216038', 'joao.silva@example.com', 'JOAO.SILVA@EXAMPLE.COM', 1, 'AQAAAAIAAYagAAAAEABbZCk3T2CqgF5NsM6VV2fIUlO5FsKkK7V2oR7KZ2IHflLJKUuH7C0sghRdzJnA==', 'dFBC5j6WGr5zBNdRZ6qkmSvTV/EiXtFXh/k7uH9LkcO=', '3d4d7d5a-9d9e-4f39-bd7f-4f7ec4c8b3f9', '11987654321', 1, 0, NULL, 1, 0),
('2', '27382886000', '27382886000', 'maria.oliveira@example.com', 'MARIA.OLIVEIRA@EXAMPLE.COM', 1, 'AQAAAAIAAYagAAAAEABt7xq7T+K3a5Vc3jVu3M9uJh2Gh4n1M8GOPU4RQWwlJoTt6WZLbB9FhZGkGn/8g==', 'xl+T4z5kM0xL3C4bxzFzlg67X/2tdzC3jH93FjOPBb4=', '9b1c00d5-7688-459e-b1ae-e6a1d81b18f0', '21998765432', 1, 0, NULL, 1, 0),
('3', '92023338077', '92023338077', 'pedro.santos@example.com', 'PEDRO.SANTOS@EXAMPLE.COM', 1, 'AQAAAAIAAYagAAAAEABgJ4Y1sGVZFHjlPscGHTR5abmGmIwY1Fmj0F7g2ZyHkfhWcXMYw0wR/Vs3D/Fw==', 'wYr9K5Y7GQ2zL4yHk/++B6GrXZbGytjXGp1r/m7XQlw=', '1d3dce76-2aef-4f4a-b9d4-04f2540d0e7b', '31987651234', 1, 0, NULL, 1, 0),
('4', '40201971054', '40201971054', 'ana.costa@example.com', 'ANA.COSTA@EXAMPLE.COM', 1, 'AQAAAAIAAYagAAAAEABgJ4Y1sGVZFHjlPscGHTR5abmGmIwY1Fmj0F7g2ZyHkfhWcXMYw0wR/Vs3D/Fw==', 'U/v8l7Op1R93VxW2oNY9fgqO8b9sP/ZsIHSc1cC4Lw==', 'a7092335-e7b4-48ff-a454-3f1c9b006e3a', '41998761234', 1, 0, NULL, 1, 0),
('49b4fd3c-edfe-4fd5-b44b-14b61492b97e', '98487027091', '98487027091', 'valdirsantana53@gmail.com', 'VALDIRSANTANA53@GMAIL.COM', '1', 'AQAAAAIAAYagAAAAENXnPg34lX7QHURAHXvQMitqf03JLLTIsos6W2EGOPSspERkT8vj+hUXeGYW2vLpYg==', 'XUPY6P7IPZIYO5H3POTGTMX2QKTXJBDW', 'f12ea623-3346-46b2-ab9f-79918f4317e9', NULL, '0', '0', NULL, '1', '0');
senha de 98487027091 - 123456789

select * from aspnetusers;
select * from aspnetroles;

INSERT INTO `aspnetuserroles` (`UserId`, `RoleId`) VALUES
('1', '1'), -- João Silva com papel ADMINISTRADOR
('2', '2'), -- Maria Oliveira com papel GESTOR
('3', '3'), -- Pedro Santos com papel COLABORADOR
('4', '4'), -- Ana Costa com papel USUARIO
('49b4fd3c-edfe-4fd5-b44b-14b61492b97e',1);

select * from aspnetuserroles;






