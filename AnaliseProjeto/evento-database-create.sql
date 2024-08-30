-- MySQL Workbench Forward Engineering

SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';

-- -----------------------------------------------------
-- Schema evento
-- -----------------------------------------------------
DROP SCHEMA IF EXISTS `evento` ;

-- -----------------------------------------------------
-- Schema evento
-- -----------------------------------------------------
CREATE SCHEMA IF NOT EXISTS `evento` DEFAULT CHARACTER SET utf8 ;
USE `evento` ;

-- -----------------------------------------------------
-- Table `evento`.`TipoEvento`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`TipoEvento` (
  `id` INT UNSIGNED NOT NULL,
  `nome` VARCHAR(50) NOT NULL,
  PRIMARY KEY (`id`))
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `evento`.`EstadosBrasil`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`EstadosBrasil` (
  `estado` CHAR(2) NOT NULL,
  `nome` VARCHAR(50) NOT NULL,
  PRIMARY KEY (`estado`))
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `evento`.`Evento`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`Evento` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `nome` VARCHAR(200) NOT NULL,
  `descricao` VARCHAR(5000) NULL,
  `dataInicio` DATETIME NULL,
  `dataFim` DATETIME NULL,
  `inscricaoGratuita` TINYINT NOT NULL DEFAULT '0',
  `status` ENUM('C', 'A', 'F') NOT NULL DEFAULT 'C' COMMENT 'C- CADASTRO\nA- ATIVO\nI - INATIVO\nF- FINALIZADO\n ',
  `dataInicioInscricao` DATETIME NULL,
  `dataFimInscricao` DATETIME NULL,
  `valorInscricao` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `website` VARCHAR(200) NULL,
  `emailEvento` VARCHAR(100) NULL,
  `eventoPublico` TINYINT NOT NULL DEFAULT 1,
  `cep` VARCHAR(8) NULL,
  `estado` CHAR(2) NULL,
  `cidade` VARCHAR(50) NULL,
  `bairro` VARCHAR(50) NULL,
  `rua` VARCHAR(50) NULL,
  `numero` VARCHAR(50) NULL,
  `complemento` VARCHAR(50) NULL,
  `possuiCertificado` TINYINT NOT NULL DEFAULT 0,
  `frequenciaMinimaCertificado` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `idTipoEvento` INT UNSIGNED NULL,
  `vagasOfertadas` INT NOT NULL DEFAULT 0,
  `vagasReservadas` INT NOT NULL DEFAULT 0,
  `vagasDisponiveis` INT NOT NULL DEFAULT 0,
  `tempoMinutosReserva` INT NOT NULL DEFAULT 15,
  `cargaHoraria` INT NOT NULL DEFAULT 0,
  `imagemPortal` BLOB NULL,
  PRIMARY KEY (`id`),
  UNIQUE INDEX `idEvento_UNIQUE` (`id` ASC),
  INDEX `fk_Evento_TipoEvento1_idx` (`idTipoEvento` ASC),
  INDEX `fk_Evento_EstadosBrasil1_idx` (`estado` ASC),
  CONSTRAINT `fk_Evento_TipoEvento1`
    FOREIGN KEY (`idTipoEvento`)
    REFERENCES `evento`.`TipoEvento` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `fk_Evento_EstadosBrasil1`
    FOREIGN KEY (`estado`)
    REFERENCES `evento`.`EstadosBrasil` (`estado`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `evento`.`Pessoa`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`Pessoa` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `nome` VARCHAR(50) NOT NULL,
  `nomeCracha` VARCHAR(20) NOT NULL,
  `cpf` VARCHAR(11) NOT NULL,
  `sexo` ENUM('M', 'F', 'N') NOT NULL DEFAULT 'N' COMMENT 'M - Masculino\nF - Feminino\nN - Não Informado',
  `cep` VARCHAR(8) NULL,
  `rua` VARCHAR(50) NULL,
  `bairro` VARCHAR(50) NULL,
  `cidade` VARCHAR(50) NULL,
  `estado` CHAR(2) NULL,
  `numero` VARCHAR(10) NULL,
  `complemento` VARCHAR(50) NULL,
  `email` VARCHAR(50) NOT NULL,
  `telefone1` VARCHAR(12) NULL,
  `telefone2` VARCHAR(12) NULL,
  `foto` BLOB NULL,
  PRIMARY KEY (`id`),
  UNIQUE INDEX `cpf_UNIQUE` (`cpf` ASC),
  INDEX `fk_Pessoa_EstadosBrasil1_idx` (`estado` ASC),
  CONSTRAINT `fk_Pessoa_EstadosBrasil1`
    FOREIGN KEY (`estado`)
    REFERENCES `evento`.`EstadosBrasil` (`estado`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `evento`.`Papel`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`Papel` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nome` VARCHAR(50) NOT NULL,
  PRIMARY KEY (`id`))
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `evento`.`TipoInscricao`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`TipoInscricao` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `idEvento` INT UNSIGNED NOT NULL,
  `nome` VARCHAR(50) NOT NULL,
  `descricao` VARCHAR(200) NOT NULL,
  `valor` DECIMAL(10,2) NOT NULL,
  `dataInicio` DATETIME NOT NULL,
  `datafim` DATETIME NOT NULL,
  `usadaEvento` TINYINT NOT NULL DEFAULT 0,
  `usadaSubevento` TINYINT NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`),
  INDEX `fk_TipoInscricao_Evento1_idx` (`idEvento` ASC),
  CONSTRAINT `fk_TipoInscricao_Evento1`
    FOREIGN KEY (`idEvento`)
    REFERENCES `evento`.`Evento` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `evento`.`InscricaoPessoaEvento`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`InscricaoPessoaEvento` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `idPessoa` INT UNSIGNED NOT NULL,
  `idEvento` INT UNSIGNED NOT NULL,
  `idPapel` INT NOT NULL,
  `idTipoInscricao` INT UNSIGNED NULL,
  `dataInscricao` DATETIME NOT NULL,
  `valorTotal` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `status` ENUM('A', 'C', 'S') NOT NULL DEFAULT 'S' COMMENT 'A - ATIVA\nC - CANCELADA\nS - SOLICITADA\n',
  `frequenciaFinal` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `nomeCracha` VARCHAR(20) NULL,
  INDEX `fk_PessoaEvento_Evento1_idx` (`idEvento` ASC),
  INDEX `fk_PessoaEvento_Pessoa_idx` (`idPessoa` ASC),
  INDEX `fk_PessoaEvento_Papel1_idx` (`idPapel` ASC),
  INDEX `fk_PessoaEvento_TipoInscricao1_idx` (`idTipoInscricao` ASC),
  PRIMARY KEY (`id`),
  CONSTRAINT `fk_PessoaEvento_Pessoa`
    FOREIGN KEY (`idPessoa`)
    REFERENCES `evento`.`Pessoa` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `fk_PessoaEvento_Evento1`
    FOREIGN KEY (`idEvento`)
    REFERENCES `evento`.`Evento` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `fk_PessoaEvento_Papel1`
    FOREIGN KEY (`idPapel`)
    REFERENCES `evento`.`Papel` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `fk_PessoaEvento_TipoInscricao1`
    FOREIGN KEY (`idTipoInscricao`)
    REFERENCES `evento`.`TipoInscricao` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `evento`.`ModeloCertificado`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`ModeloCertificado` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `logotipoSuperior` BLOB NOT NULL,
  `textoAntesParticipante` VARCHAR(500) NOT NULL,
  `textoAntesEvento` VARCHAR(500) NOT NULL,
  `textoAntesCargaHoraria` VARCHAR(500) NOT NULL,
  `assinatura1Texto` VARCHAR(50) NOT NULL,
  `assinatura1` BLOB NOT NULL,
  `assinatura2Texto` VARCHAR(50) NULL,
  `assinatura2` BLOB NULL,
  `idEvento` INT UNSIGNED NOT NULL,
  PRIMARY KEY (`id`),
  INDEX `fk_ModeloCertificado_Evento1_idx` (`idEvento` ASC),
  CONSTRAINT `fk_ModeloCertificado_Evento1`
    FOREIGN KEY (`idEvento`)
    REFERENCES `evento`.`Evento` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `evento`.`ModeloCracha`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`ModeloCracha` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `logotipo` BLOB NOT NULL,
  `texto` VARCHAR(200) NOT NULL,
  `qrcode` TINYINT NOT NULL DEFAULT 0,
  `idEvento` INT UNSIGNED NOT NULL,
  PRIMARY KEY (`id`),
  INDEX `fk_ModeloCracha_Evento1_idx` (`idEvento` ASC),
  CONSTRAINT `fk_ModeloCracha_Evento1`
    FOREIGN KEY (`idEvento`)
    REFERENCES `evento`.`Evento` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `evento`.`AreaInteresse`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`AreaInteresse` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `nome` VARCHAR(100) NOT NULL,
  PRIMARY KEY (`id`))
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `evento`.`AreaInteresseEvento`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`AreaInteresseEvento` (
  `idAreaInteresse` INT UNSIGNED NOT NULL,
  `idEvento` INT UNSIGNED NOT NULL,
  PRIMARY KEY (`idAreaInteresse`, `idEvento`),
  INDEX `fk_AreaInteresseEvento_Evento1_idx` (`idEvento` ASC),
  INDEX `fk_AreaInteresseEvento_AreaInteresse1_idx` (`idAreaInteresse` ASC),
  CONSTRAINT `fk_AreaInteresseEvento_AreaInteresse1`
    FOREIGN KEY (`idAreaInteresse`)
    REFERENCES `evento`.`AreaInteresse` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `fk_AreaInteresseEvento_Evento1`
    FOREIGN KEY (`idEvento`)
    REFERENCES `evento`.`Evento` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `evento`.`PessoaAreaInteresse`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`PessoaAreaInteresse` (
  `idPessoa` INT UNSIGNED NOT NULL,
  `idAreaInteresse` INT UNSIGNED NOT NULL,
  `todosEstados` TINYINT NOT NULL DEFAULT 1,
  PRIMARY KEY (`idPessoa`, `idAreaInteresse`),
  INDEX `fk_PessoaAreaInteresse_AreaInteresse1_idx` (`idAreaInteresse` ASC),
  INDEX `fk_PessoaAreaInteresse_Pessoa1_idx` (`idPessoa` ASC),
  CONSTRAINT `fk_PessoaAreaInteresse_Pessoa1`
    FOREIGN KEY (`idPessoa`)
    REFERENCES `evento`.`Pessoa` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `fk_PessoaAreaInteresse_AreaInteresse1`
    FOREIGN KEY (`idAreaInteresse`)
    REFERENCES `evento`.`AreaInteresse` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `evento`.`SubEvento`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`SubEvento` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `idEvento` INT UNSIGNED NOT NULL,
  `nome` VARCHAR(200) NOT NULL,
  `descricao` VARCHAR(5000) NOT NULL,
  `dataInicio` DATETIME NOT NULL,
  `dataFim` DATETIME NOT NULL,
  `inscricaoGratuita` TINYINT NOT NULL DEFAULT '0',
  `status` ENUM('C', 'A', 'F') NOT NULL DEFAULT 'C' COMMENT 'C- CADASTRO\nA- ABERTO\nF- FINALIZADO\n ',
  `dataInicioInscricao` DATETIME NOT NULL,
  `dataFimInscricao` DATETIME NOT NULL,
  `valorInscricao` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `possuiCertificado` TINYINT NOT NULL DEFAULT 0,
  `frequenciaMinimaCertificado` DECIMAL(10,2) UNSIGNED NOT NULL DEFAULT 0,
  `vagasOfertadas` INT UNSIGNED NOT NULL DEFAULT 0,
  `vagasReservadas` INT UNSIGNED NOT NULL DEFAULT 0,
  `vagasDisponiveis` INT UNSIGNED NOT NULL DEFAULT 0,
  `cargaHoraria` INT UNSIGNED NOT NULL DEFAULT 0,
  `idTipoEvento` INT UNSIGNED NOT NULL,
  PRIMARY KEY (`id`),
  INDEX `fk_SubEvento_Evento1_idx` (`idEvento` ASC),
  INDEX `fk_SubEvento_TipoEvento1_idx` (`idTipoEvento` ASC),
  CONSTRAINT `fk_SubEvento_Evento1`
    FOREIGN KEY (`idEvento`)
    REFERENCES `evento`.`Evento` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `fk_SubEvento_TipoEvento1`
    FOREIGN KEY (`idTipoEvento`)
    REFERENCES `evento`.`TipoEvento` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `evento`.`InscricaoPessoaSubEvento`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`InscricaoPessoaSubEvento` (
  `idPessoa` INT UNSIGNED NOT NULL,
  `idSubEvento` INT UNSIGNED NOT NULL,
  `idPapel` INT NOT NULL,
  `idTipoInscricao` INT UNSIGNED NULL,
  `dataInscricao` DATETIME NOT NULL,
  `valor` DECIMAL(10,2) NOT NULL DEFAULT 0,
  `status` ENUM('A', 'C', 'S') NOT NULL DEFAULT 'S' COMMENT 'A - ATIVA\nC - CANCELADA\nS - SOLICITADA\n',
  `frequenciaFinal` DECIMAL(10,2) NOT NULL DEFAULT 0,
  PRIMARY KEY (`idPessoa`, `idSubEvento`),
  INDEX `fk_PessoaSubEvento_SubEvento1_idx` (`idSubEvento` ASC),
  INDEX `fk_PessoaSubEvento_Pessoa1_idx` (`idPessoa` ASC),
  INDEX `fk_InscricaoPessoaSubEvento_Papel1_idx` (`idPapel` ASC),
  INDEX `fk_InscricaoPessoaSubEvento_TipoInscricao1_idx` (`idTipoInscricao` ASC),
  CONSTRAINT `fk_PessoaSubEvento_Pessoa1`
    FOREIGN KEY (`idPessoa`)
    REFERENCES `evento`.`Pessoa` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `fk_PessoaSubEvento_SubEvento1`
    FOREIGN KEY (`idSubEvento`)
    REFERENCES `evento`.`SubEvento` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `fk_InscricaoPessoaSubEvento_Papel1`
    FOREIGN KEY (`idPapel`)
    REFERENCES `evento`.`Papel` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `fk_InscricaoPessoaSubEvento_TipoInscricao1`
    FOREIGN KEY (`idTipoInscricao`)
    REFERENCES `evento`.`TipoInscricao` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `evento`.`ParticipacaoPessoaEvento`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`ParticipacaoPessoaEvento` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `idPessoa` INT UNSIGNED NOT NULL,
  `idEvento` INT UNSIGNED NOT NULL,
  `entrada` DATETIME NOT NULL,
  `saida` DATETIME NULL,
  INDEX `fk_PessoaEvento1_Evento1_idx` (`idEvento` ASC),
  INDEX `fk_PessoaEvento1_Pessoa1_idx` (`idPessoa` ASC),
  PRIMARY KEY (`id`),
  CONSTRAINT `fk_PessoaEvento1_Pessoa1`
    FOREIGN KEY (`idPessoa`)
    REFERENCES `evento`.`Pessoa` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `fk_PessoaEvento1_Evento1`
    FOREIGN KEY (`idEvento`)
    REFERENCES `evento`.`Evento` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `evento`.`ParticipacaoPessoaSubEvento`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`ParticipacaoPessoaSubEvento` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `idPessoa` INT UNSIGNED NOT NULL,
  `idSubEvento` INT UNSIGNED NOT NULL,
  `entrada` DATETIME NOT NULL,
  `saida` DATETIME NULL,
  PRIMARY KEY (`id`),
  INDEX `fk_PessoaSubEvento_SubEvento2_idx` (`idSubEvento` ASC),
  INDEX `fk_PessoaSubEvento_Pessoa2_idx` (`idPessoa` ASC),
  CONSTRAINT `fk_PessoaSubEvento_Pessoa2`
    FOREIGN KEY (`idPessoa`)
    REFERENCES `evento`.`Pessoa` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `fk_PessoaSubEvento_SubEvento2`
    FOREIGN KEY (`idSubEvento`)
    REFERENCES `evento`.`SubEvento` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `evento`.`TipoInscricaoSubEvento`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`TipoInscricaoSubEvento` (
  `idTipoInscricao` INT UNSIGNED NOT NULL,
  `idSubEvento` INT UNSIGNED NOT NULL,
  PRIMARY KEY (`idTipoInscricao`, `idSubEvento`),
  INDEX `fk_TipoInscricaoSubEvento_SubEvento1_idx` (`idSubEvento` ASC),
  INDEX `fk_TipoInscricaoSubEvento_TipoInscricao1_idx` (`idTipoInscricao` ASC),
  CONSTRAINT `fk_TipoInscricaoSubEvento_TipoInscricao1`
    FOREIGN KEY (`idTipoInscricao`)
    REFERENCES `evento`.`TipoInscricao` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `fk_TipoInscricaoSubEvento_SubEvento1`
    FOREIGN KEY (`idSubEvento`)
    REFERENCES `evento`.`SubEvento` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `evento`.`Pagamento`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`Pagamento` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `idInscricaoPessoaEvento` INT UNSIGNED NOT NULL,
  `valor` DECIMAL(10,2) NOT NULL,
  `status` ENUM('S', 'C') NOT NULL DEFAULT 'S' COMMENT 'S- SOLICITADO\nC- CONFIRMADO',
  `forma` ENUM('D', 'C', 'B', 'P') NOT NULL DEFAULT 'P' COMMENT 'D - DINHEIRO\nC- CARTAO\nB - BOLETO\nP - PIX',
  `codigoPagamento` VARCHAR(500) NULL,
  `dataPagamento` DATETIME NOT NULL,
  `dataSolicitacao` DATETIME NOT NULL,
  PRIMARY KEY (`id`),
  INDEX `fk_Pagamento_InscricaoPessoaEvento1_idx` (`idInscricaoPessoaEvento` ASC),
  CONSTRAINT `fk_Pagamento_InscricaoPessoaEvento1`
    FOREIGN KEY (`idInscricaoPessoaEvento`)
    REFERENCES `evento`.`InscricaoPessoaEvento` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `evento`.`EstadosBrasilPessoaAreaInteresse`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`EstadosBrasilPessoaAreaInteresse` (
  `estadoEstadosBrasil` CHAR(2) NOT NULL,
  `idPessoaPessoaAreaInteresse` INT UNSIGNED NOT NULL,
  `idAreaInteressePessoaAreaInteresse` INT UNSIGNED NOT NULL,
  PRIMARY KEY (`estadoEstadosBrasil`, `idPessoaPessoaAreaInteresse`, `idAreaInteressePessoaAreaInteresse`),
  INDEX `fk_EstadosBrasilPessoaAreaInteresse_PessoaAreaInteresse1_idx` (`idPessoaPessoaAreaInteresse` ASC, `idAreaInteressePessoaAreaInteresse` ASC),
  INDEX `fk_EstadosBrasilPessoaAreaInteresse_EstadosBrasil1_idx` (`estadoEstadosBrasil` ASC),
  CONSTRAINT `fk_EstadosBrasilPessoaAreaInteresse_EstadosBrasil1`
    FOREIGN KEY (`estadoEstadosBrasil`)
    REFERENCES `evento`.`EstadosBrasil` (`estado`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `fk_EstadosBrasilPessoaAreaInteresse_PessoaAreaInteresse1`
    FOREIGN KEY (`idPessoaPessoaAreaInteresse` , `idAreaInteressePessoaAreaInteresse`)
    REFERENCES `evento`.`PessoaAreaInteresse` (`idPessoa` , `idAreaInteresse`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB;


SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;

-- -----------------------------------------------------
-- Data for table `evento`.`TipoEvento`
-- -----------------------------------------------------
START TRANSACTION;
USE `evento`;
INSERT INTO `evento`.`TipoEvento` (`id`, `nome`) VALUES (1, 'Palestra');
INSERT INTO `evento`.`TipoEvento` (`id`, `nome`) VALUES (2, 'Mini-curso');
INSERT INTO `evento`.`TipoEvento` (`id`, `nome`) VALUES (3, 'Simpósio');
INSERT INTO `evento`.`TipoEvento` (`id`, `nome`) VALUES (4, 'Congresso');
INSERT INTO `evento`.`TipoEvento` (`id`, `nome`) VALUES (5, 'Workshop');
INSERT INTO `evento`.`TipoEvento` (`id`, `nome`) VALUES (6, 'Show');

COMMIT;


-- -----------------------------------------------------
-- Data for table `evento`.`EstadosBrasil`
-- -----------------------------------------------------
START TRANSACTION;
USE `evento`;
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

COMMIT;


-- -----------------------------------------------------
-- Data for table `evento`.`Papel`
-- -----------------------------------------------------
START TRANSACTION;
USE `evento`;
INSERT INTO `evento`.`Papel` (`id`, `nome`) VALUES (1, 'Gestor do Sistema');
INSERT INTO `evento`.`Papel` (`id`, `nome`) VALUES (2, 'Gestor do Evento');
INSERT INTO `evento`.`Papel` (`id`, `nome`) VALUES (3, 'Colaborador');
INSERT INTO `evento`.`Papel` (`id`, `nome`) VALUES (4, 'Participante');

COMMIT;


-- -----------------------------------------------------
-- Data for table `evento`.`AreaInteresse`
-- -----------------------------------------------------
START TRANSACTION;
USE `evento`;
INSERT INTO `evento`.`AreaInteresse` (`id`, `nome`) VALUES (1, 'Show Musical');
INSERT INTO `evento`.`AreaInteresse` (`id`, `nome`) VALUES (2, 'Tecnologia');
INSERT INTO `evento`.`AreaInteresse` (`id`, `nome`) VALUES (3, 'Saúde');
INSERT INTO `evento`.`AreaInteresse` (`id`, `nome`) VALUES (4, 'Exposição Cultural');

COMMIT;


-- -----------------------------------------------------
-- Data for table `Evento`
-- -----------------------------------------------------
START TRANSACTION;
INSERT INTO `Evento` (`id`, `nome`, `descricao`, `dataInicio`, `dataFim`, `inscricaoGratuita`, `status`, `dataInicioInscricao`, `dataFimInscricao`, `valorInscricao`, `website`, `emailEvento`, `eventoPublico`, `cep`, `estado`, `cidade`, `bairro`, `rua`, `numero`, `complemento`, `possuiCertificado`, `frequenciaMinimaCertificado`, `idTipoEvento`, `vagasOfertadas`, `vagasReservadas`, `vagasDisponiveis`, `tempoMinutosReserva`, `cargaHoraria`)
VALUES
(1, 'Tech Summit', 'Evento de tecnologia com palestras e workshops.', '2024-09-01 09:00:00', '2024-09-03 18:00:00', 1, 'A', '2024-08-01 08:00:00', '2024-08-31 23:59:00', 0, 'http://techsummit.com', 'info@techsummit.com', 1, '01001000', 'SP', 'São Paulo', 'Centro', 'Av. Paulista', '123', 'Sala 10', 1, 75, 1, 100, 50, 50, 15, 16),
(2, 'Business Expo', 'Feira de negócios com exposições e networking.', '2024-10-10 10:00:00', '2024-10-12 17:00:00', 0, 'C', '2024-09-01 09:00:00', '2024-09-30 23:59:00', 150, 'http://businessexpo.com', 'contact@businessexpo.com', 1, '20040000', 'RJ', 'Rio de Janeiro', 'Centro', 'Rua das Flores', '456', '', 1, 85, 2, 200, 60, 140, 20, 24),
(3, 'Science Congress', 'Congresso científico com apresentação de trabalhos.', '2024-11-15 09:00:00', '2024-11-17 18:00:00', 1, 'A', '2024-10-01 08:00:00', '2024-10-31 23:59:00', 0, 'http://sciencecongress.com', 'congress@science.com', 1, '30120000', 'MG', 'Belo Horizonte', 'Savassi', 'Rua dos Cientistas', '789', 'Ap. 101', 1, 90, 3, 150, 80, 70, 25, 32),
(4, 'Health Seminar', 'Seminário sobre saúde e bem-estar.', '2024-12-05 09:00:00', '2024-12-06 17:00:00', 0, 'C', '2024-11-01 09:00:00', '2024-11-30 23:59:00', 80, 'http://healthseminar.com', 'seminar@health.com', 1, '40010000', 'RS', 'Porto Alegre', 'Moinhos', 'Av. Ipiranga', '321', 'Bloco B', 0, 0, 4, 120, 30, 90, 30, 8),
(5, 'Startup Fair', 'Feira de startups e investidores.', '2025-01-20 10:00:00', '2025-01-22 18:00:00', 0, 'F', '2024-12-01 09:00:00', '2024-12-31 23:59:00', 200, 'http://startupfair.com', 'info@startupfair.com', 1, '50030000', 'BA', 'Salvador', 'Barra', 'Rua das Startups', '654', '', 1, 100, 5, 150, 70, 80, 45, 12);

COMMIT;


-- -----------------------------------------------------
-- Data for table `Pessoa`
-- -----------------------------------------------------
START TRANSACTION;
INSERT INTO `Pessoa` (`id`, `nome`, `nomeCracha`, `cpf`, `sexo`, `cep`, `rua`, `bairro`, `cidade`, `estado`, `numero`, `complemento`, `email`, `telefone1`, `telefone2`)
VALUES
(1, 'João da Silva', 'João', '12345678901', 'M', '01002000', 'Rua A', 'Centro', 'São Paulo', 'SP', '10', '', 'joao.silva@example.com', '11987654321', NULL),
(2, 'Maria Santos', 'Maria', '23456789012', 'F', '20030000', 'Rua B', 'Botafogo', 'Rio de Janeiro', 'RJ', '20', 'Ap. 2', 'maria.santos@example.com', '21987654321', NULL),
(3, 'Carlos Pereira', 'Carlos', '34567890123', 'M', '30110000', 'Rua C', 'Centro', 'Belo Horizonte', 'MG', '30', '', 'carlos.pereira@example.com', '31987654321', NULL),
(4, 'Ana Costa', 'Ana', '45678901234', 'F', '40020000', 'Rua D', 'Centro', 'Porto Alegre', 'RS', '40', 'Bloco C', 'ana.costa@example.com', '51987654321', NULL),
(5, 'Pedro Souza', 'Pedro', '56789012345', 'M', '50040000', 'Rua E', 'Barra', 'Salvador', 'BA', '50', '', 'pedro.souza@example.com', '71987654321', NULL);

COMMIT;


-- -----------------------------------------------------
-- Data for table `TipoInscricao`
-- -----------------------------------------------------
START TRANSACTION;
INSERT INTO `TipoInscricao` (`id`, `idEvento`, `nome`, `descricao`, `valor`, `dataInicio`, `datafim`, `usadaEvento`, `usadaSubevento`)
VALUES
(1, 1, 'Padrão', 'Inscrição gratuita', 0, '2024-08-01 08:00:00', '2024-08-31 23:59:00', 1, 0),
(2, 2, 'VIP', 'Acesso completo ao evento', 300, '2024-09-01 09:00:00', '2024-09-30 23:59:00', 1, 0),
(3, 3, 'Estudante', 'Desconto para estudantes', 50, '2024-10-01 08:00:00', '2024-10-31 23:59:00', 1, 0),
(4, 4, 'Online', 'Acesso apenas virtual', 100, '2024-11-01 09:00:00', '2024-11-30 23:59:00', 1, 0),
(5, 5, 'Premium', 'Acesso premium com benefícios', 500, '2024-12-01 09:00:00', '2024-12-31 23:59:00', 1, 0);

COMMIT;


-- -----------------------------------------------------
-- Data for table `InscricaoPessoaEvento`
-- -----------------------------------------------------
START TRANSACTION;
INSERT INTO `InscricaoPessoaEvento` (`id`, `idPessoa`, `idEvento`, `idPapel`, `idTipoInscricao`, `dataInscricao`, `valorTotal`, `status`, `frequenciaFinal`, `nomeCracha`)
VALUES
(1, 1, 1, 1, 1, '2024-08-05 10:00:00', 0, 'A', 80, 'João'),
(2, 2, 2, 2, 2, '2024-09-10 11:00:00', 300, 'S', 0, 'Maria'),
(3, 3, 3, 3, 3, '2024-10-15 12:00:00', 50, 'C', 0, 'Carlos'),
(4, 4, 4, 4, 4, '2024-11-20 13:00:00', 100, 'P', 0, 'Ana'),
(5, 5, 5, 5, 5, '2024-12-25 14:00:00', 500, 'A', 95, 'Pedro');

COMMIT;