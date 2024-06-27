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
CREATE SCHEMA IF NOT EXISTS `evento` DEFAULT CHARACTER SET utf8mb3 ;
USE `evento` ;

-- -----------------------------------------------------
-- Table `evento`.`areainteresse`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`areainteresse` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `nome` VARCHAR(100) NOT NULL,
  PRIMARY KEY (`id`))
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb3;


-- -----------------------------------------------------
-- Table `evento`.`estadosbrasil`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`estadosbrasil` (
  `estado` CHAR(2) NOT NULL,
  `nome` VARCHAR(50) NOT NULL,
  PRIMARY KEY (`estado`))
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb3;


-- -----------------------------------------------------
-- Table `evento`.`tipoevento`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`tipoevento` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `nome` VARCHAR(50) NOT NULL,
  PRIMARY KEY (`id`))
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb3;


-- -----------------------------------------------------
-- Table `evento`.`evento`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`evento` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `nome` VARCHAR(200) NOT NULL,
  `descricao` VARCHAR(5000) NOT NULL,
  `dataInicio` DATETIME NOT NULL,
  `dataFim` DATETIME NOT NULL,
  `inscricaoGratuita` TINYINT NOT NULL DEFAULT '0',
  `status` ENUM('C', 'A', 'F') NOT NULL DEFAULT 'C' COMMENT 'C- CADASTRO\\nA- ATIVO\\nI - INATIVO\\nF- FINALIZADO\\n ',
  `dataInicioInscricao` DATETIME NOT NULL,
  `dataFimInscricao` DATETIME NOT NULL,
  `valorInscricaoMaisBarata` DECIMAL(10,2) NOT NULL DEFAULT '0.00',
  `website` VARCHAR(200) NULL DEFAULT NULL,
  `emailEvento` VARCHAR(100) NULL DEFAULT NULL,
  `eventoPublico` TINYINT NOT NULL DEFAULT '1',
  `cep` VARCHAR(8) NOT NULL,
  `estado` CHAR(2) NOT NULL,
  `cidade` VARCHAR(50) NOT NULL,
  `bairro` VARCHAR(50) NOT NULL,
  `rua` VARCHAR(50) NOT NULL,
  `numero` VARCHAR(50) NULL DEFAULT NULL,
  `complemento` VARCHAR(50) NULL DEFAULT NULL,
  `possuiCertificado` TINYINT NOT NULL DEFAULT '0',
  `frequenciaMinimaCertificado` DECIMAL(10,2) NOT NULL DEFAULT '0.00',
  `vagasOfertadas` INT NOT NULL DEFAULT '0',
  `vagasReservadas` INT NOT NULL DEFAULT '0',
  `vagasDisponiveis` INT NOT NULL DEFAULT '0',
  `tempoMinutosReserva` INT NOT NULL DEFAULT '15',
  `cargaHoraria` INT NOT NULL DEFAULT '0',
  `idTipoEvento` INT UNSIGNED NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE INDEX `idEvento_UNIQUE` (`id` ASC) VISIBLE,
  INDEX `fk_Evento_EstadosBrasil1_idx` (`estado` ASC) VISIBLE,
  INDEX `fk_Evento_TipoEvento1_idx` (`idTipoEvento` ASC) VISIBLE,
  CONSTRAINT `fk_Evento_EstadosBrasil1`
    FOREIGN KEY (`estado`)
    REFERENCES `evento`.`estadosbrasil` (`estado`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `fk_Evento_TipoEvento1`
    FOREIGN KEY (`idTipoEvento`)
    REFERENCES `evento`.`tipoevento` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb3;


-- -----------------------------------------------------
-- Table `evento`.`areainteresseevento`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`areainteresseevento` (
  `idAreaInteresse` INT UNSIGNED NOT NULL,
  `idEvento` INT UNSIGNED NOT NULL,
  PRIMARY KEY (`idAreaInteresse`, `idEvento`),
  INDEX `fk_AreaInteresseEvento_Evento1_idx` (`idEvento` ASC) VISIBLE,
  INDEX `fk_AreaInteresseEvento_AreaInteresse1_idx` (`idAreaInteresse` ASC) VISIBLE,
  CONSTRAINT `fk_AreaInteresseEvento_AreaInteresse1`
    FOREIGN KEY (`idAreaInteresse`)
    REFERENCES `evento`.`areainteresse` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `fk_AreaInteresseEvento_Evento1`
    FOREIGN KEY (`idEvento`)
    REFERENCES `evento`.`evento` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb3;


-- -----------------------------------------------------
-- Table `evento`.`papel`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`papel` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `nome` VARCHAR(50) NOT NULL,
  PRIMARY KEY (`id`))
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb3;


-- -----------------------------------------------------
-- Table `evento`.`tipoinscricao`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`tipoinscricao` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `idEvento` INT UNSIGNED NOT NULL,
  `nome` VARCHAR(100) NOT NULL,
  `descricao` VARCHAR(300) NOT NULL,
  `valor` DECIMAL(10,2) NOT NULL,
  `dataInicio` DATETIME NOT NULL,
  `dataFim` DATETIME NOT NULL,
  `usadaEvento` TINYINT NOT NULL DEFAULT '1',
  `usadaSubevento` TINYINT NOT NULL DEFAULT '1',
  PRIMARY KEY (`id`),
  INDEX `fk_TipoInscricao_Evento1_idx` (`idEvento` ASC) VISIBLE,
  CONSTRAINT `fk_TipoInscricao_Evento1`
    FOREIGN KEY (`idEvento`)
    REFERENCES `evento`.`evento` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb3;


-- -----------------------------------------------------
-- Table `evento`.`pessoa`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`pessoa` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `nome` VARCHAR(50) NOT NULL,
  `nomeCracha` VARCHAR(20) NOT NULL,
  `cpf` VARCHAR(11) NOT NULL,
  `sexo` ENUM('M', 'F', 'N') NOT NULL DEFAULT 'N' COMMENT 'M - Masculino\\nF - Feminino\\nN - Não Informado',
  `cep` VARCHAR(8) NULL DEFAULT NULL,
  `rua` VARCHAR(50) NULL DEFAULT NULL,
  `bairro` VARCHAR(50) NULL DEFAULT NULL,
  `cidade` VARCHAR(50) NULL DEFAULT NULL,
  `estado` CHAR(2) NULL DEFAULT NULL,
  `numero` VARCHAR(10) NULL DEFAULT NULL,
  `complemento` VARCHAR(50) NULL DEFAULT NULL,
  `email` VARCHAR(50) NOT NULL,
  `telefone1` VARCHAR(10) NULL DEFAULT NULL,
  `telefone2` VARCHAR(10) NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE INDEX `cpf_UNIQUE` (`cpf` ASC) VISIBLE,
  INDEX `fk_Pessoa_EstadosBrasil1_idx` (`estado` ASC) VISIBLE,
  CONSTRAINT `fk_Pessoa_EstadosBrasil1`
    FOREIGN KEY (`estado`)
    REFERENCES `evento`.`estadosbrasil` (`estado`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb3;


-- -----------------------------------------------------
-- Table `evento`.`inscricaopessoaevento`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`inscricaopessoaevento` (
  `idPessoa` INT UNSIGNED NOT NULL,
  `idEvento` INT UNSIGNED NOT NULL,
  `idPapel` INT UNSIGNED NOT NULL,
  `idTipoInscricao` INT UNSIGNED NOT NULL,
  `dataInscricao` DATETIME NOT NULL,
  `valorTotal` DECIMAL(10,2) NOT NULL DEFAULT '0.00',
  `status` ENUM('A', 'C', 'S') NOT NULL DEFAULT 'S' COMMENT 'A - ATIVA\\nC - CANCELADA\\nS - SOLICITADA\\n',
  `frequenciaFinal` DECIMAL(10,2) NOT NULL DEFAULT '0.00',
  PRIMARY KEY (`idPessoa`, `idEvento`),
  INDEX `fk_PessoaEvento_Evento1_idx` (`idEvento` ASC) VISIBLE,
  INDEX `fk_PessoaEvento_Pessoa_idx` (`idPessoa` ASC) VISIBLE,
  INDEX `fk_InscricaoPessoaEvento_Papel1_idx` (`idPapel` ASC) VISIBLE,
  INDEX `fk_InscricaoPessoaEvento_TipoInscricao1_idx` (`idTipoInscricao` ASC) VISIBLE,
  CONSTRAINT `fk_InscricaoPessoaEvento_Papel1`
    FOREIGN KEY (`idPapel`)
    REFERENCES `evento`.`papel` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `fk_InscricaoPessoaEvento_TipoInscricao1`
    FOREIGN KEY (`idTipoInscricao`)
    REFERENCES `evento`.`tipoinscricao` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `fk_PessoaEvento_Evento1`
    FOREIGN KEY (`idEvento`)
    REFERENCES `evento`.`evento` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `fk_PessoaEvento_Pessoa`
    FOREIGN KEY (`idPessoa`)
    REFERENCES `evento`.`pessoa` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb3;


-- -----------------------------------------------------
-- Table `evento`.`modelocertificado`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`modelocertificado` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `logotipoSuperior` BLOB NOT NULL,
  `textoAntesParticipante` VARCHAR(500) NOT NULL,
  `textoAntesEvento` VARCHAR(500) NOT NULL,
  `textoAntesCargaHoraria` VARCHAR(500) NOT NULL,
  `assinatura1Texto` VARCHAR(50) NOT NULL,
  `assinatura1` BLOB NOT NULL,
  `assinatura2Texto` VARCHAR(50) NULL DEFAULT NULL,
  `assinatura2` BLOB NULL DEFAULT NULL,
  `idEvento` INT UNSIGNED NOT NULL,
  PRIMARY KEY (`id`),
  INDEX `fk_ModeloCertificado_Evento1_idx` (`idEvento` ASC) VISIBLE,
  CONSTRAINT `fk_ModeloCertificado_Evento1`
    FOREIGN KEY (`idEvento`)
    REFERENCES `evento`.`evento` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb3;


-- -----------------------------------------------------
-- Table `evento`.`modelocracha`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`modelocracha` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `logotipo` BLOB NOT NULL,
  `texto` VARCHAR(200) NOT NULL,
  `qrcode` TINYINT NOT NULL DEFAULT '0',
  `idEvento` INT UNSIGNED NOT NULL,
  PRIMARY KEY (`id`),
  INDEX `fk_ModeloCracha_Evento1_idx` (`idEvento` ASC) VISIBLE,
  CONSTRAINT `fk_ModeloCracha_Evento1`
    FOREIGN KEY (`idEvento`)
    REFERENCES `evento`.`evento` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb3;


-- -----------------------------------------------------
-- Table `evento`.`pessoaareainteresse`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`pessoaareainteresse` (
  `idPessoa` INT UNSIGNED NOT NULL,
  `idAreaInteresse` INT UNSIGNED NOT NULL,
  `todosEstados` TINYINT NOT NULL DEFAULT '1',
  PRIMARY KEY (`idPessoa`, `idAreaInteresse`),
  INDEX `fk_PessoaAreaInteresse_AreaInteresse1_idx` (`idAreaInteresse` ASC) VISIBLE,
  INDEX `fk_PessoaAreaInteresse_Pessoa1_idx` (`idPessoa` ASC) VISIBLE,
  CONSTRAINT `fk_PessoaAreaInteresse_AreaInteresse1`
    FOREIGN KEY (`idAreaInteresse`)
    REFERENCES `evento`.`areainteresse` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `fk_PessoaAreaInteresse_Pessoa1`
    FOREIGN KEY (`idPessoa`)
    REFERENCES `evento`.`pessoa` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb3;


-- -----------------------------------------------------
-- Table `evento`.`subevento`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `evento`.`subevento` (
  `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `idEvento` INT UNSIGNED NOT NULL,
  `nome` VARCHAR(200) NOT NULL,
  `descricao` VARCHAR(5000) NOT NULL,
  `dataInicio` DATETIME NOT NULL,
  `dataFim` DATETIME NOT NULL,
  `inscricaoGratuita` TINYINT NOT NULL DEFAULT '0',
  `status` ENUM('C', 'A', 'F') NOT NULL DEFAULT 'C' COMMENT 'C- CADASTRO\\nA- ABERTO\\nF- FINALIZADO\\n ',
  `dataInicioInscricao` DATETIME NOT NULL,
  `dataFimInscricao` DATETIME NOT NULL,
  `valorInscricaoMaisBarata` DECIMAL(10,2) NOT NULL DEFAULT '0.00',
  `possuiCertificado` TINYINT NOT NULL DEFAULT '0',
  `frequenciaMinimaCertificado` DECIMAL(10,2) NOT NULL DEFAULT '0.00',
  `vagasOfertadas` INT NOT NULL DEFAULT '0',
  `vagasReservadas` INT NOT NULL DEFAULT '0',
  `vagasDisponiveis` INT NOT NULL,
  `cargaHoraria` INT NOT NULL DEFAULT '0',
  `idTipoEvento` INT UNSIGNED NOT NULL,
  PRIMARY KEY (`id`),
  INDEX `fk_SubEvento_Evento1_idx` (`idEvento` ASC) VISIBLE,
  INDEX `fk_SubEvento_TipoEvento1_idx` (`idTipoEvento` ASC) VISIBLE,
  CONSTRAINT `fk_SubEvento_Evento1`
    FOREIGN KEY (`idEvento`)
    REFERENCES `evento`.`evento` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `fk_SubEvento_TipoEvento1`
    FOREIGN KEY (`idTipoEvento`)
    REFERENCES `evento`.`tipoevento` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb3;


SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
