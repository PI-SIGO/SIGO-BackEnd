-- SIGO database schema for MySQL Workbench.
-- Generated from the project class diagram and current EF Core model.
-- Target: MySQL 8.0+ / InnoDB / utf8mb4.

SET @OLD_UNIQUE_CHECKS = @@UNIQUE_CHECKS, UNIQUE_CHECKS = 0;
SET @OLD_FOREIGN_KEY_CHECKS = @@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS = 0;
SET @OLD_SQL_MODE = @@SQL_MODE, SQL_MODE = 'STRICT_TRANS_TABLES,NO_ENGINE_SUBSTITUTION';

CREATE DATABASE IF NOT EXISTS `sigo`
  DEFAULT CHARACTER SET utf8mb4
  DEFAULT COLLATE utf8mb4_unicode_ci;

USE `sigo`;

CREATE TABLE IF NOT EXISTS `cliente` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nome` VARCHAR(100) NOT NULL,
  `email` VARCHAR(100) NOT NULL,
  `senha` VARCHAR(100) NOT NULL,
  `cpf_cnpj` VARCHAR(14) NOT NULL,
  `obs` VARCHAR(500) NULL,
  `razao` VARCHAR(500) NULL,
  `datanasc` DATE NULL,
  `sexo` INT NOT NULL,
  `numero` INT NOT NULL,
  `rua` VARCHAR(500) NOT NULL,
  `cidade` VARCHAR(500) NOT NULL,
  `cep` VARCHAR(20) NOT NULL,
  `bairro` VARCHAR(500) NOT NULL,
  `estado` VARCHAR(500) NOT NULL,
  `pais` VARCHAR(500) NOT NULL,
  `complemento` VARCHAR(500) NOT NULL,
  `tipocliente` INT NOT NULL,
  `situacao` INT NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `UX_cliente_email` (`email`),
  UNIQUE KEY `UX_cliente_cpf_cnpj` (`cpf_cnpj`),
  CONSTRAINT `CK_cliente_sexo` CHECK (`sexo` IN (1, 2, 3)),
  CONSTRAINT `CK_cliente_tipocliente` CHECK (`tipocliente` IN (1, 2)),
  CONSTRAINT `CK_cliente_situacao` CHECK (`situacao` IN (1, 2))
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `oficina` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nome` VARCHAR(100) NOT NULL,
  `cnpj` VARCHAR(18) NOT NULL,
  `email` VARCHAR(100) NOT NULL,
  `numero` INT NOT NULL,
  `rua` VARCHAR(200) NOT NULL,
  `cidade` VARCHAR(100) NOT NULL,
  `cep` INT NOT NULL,
  `bairro` VARCHAR(100) NOT NULL,
  `estado` VARCHAR(50) NOT NULL,
  `pais` VARCHAR(50) NOT NULL,
  `complemento` VARCHAR(200) NULL,
  `senha` TEXT NOT NULL,
  `situacao` INT NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `IX_oficina_cnpj` (`cnpj`),
  UNIQUE KEY `IX_oficina_email` (`email`),
  CONSTRAINT `CK_oficina_situacao` CHECK (`situacao` IN (1, 2))
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `servico` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nome` VARCHAR(100) NOT NULL,
  `descricao` TEXT NOT NULL,
  `valor` DECIMAL(18, 2) NOT NULL,
  `garantia` DATE NOT NULL,
  `id_oficina` INT NULL,
  PRIMARY KEY (`id`),
  KEY `IX_servico_id_oficina_nome` (`id_oficina`, `nome`),
  CONSTRAINT `FK_servico_oficina`
    FOREIGN KEY (`id_oficina`)
    REFERENCES `oficina` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `funcionario` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nome` VARCHAR(100) NOT NULL,
  `cpf` VARCHAR(12) NOT NULL,
  `cargo` VARCHAR(100) NOT NULL,
  `email` VARCHAR(100) NOT NULL,
  `senha` TEXT NOT NULL,
  `role` VARCHAR(30) NOT NULL DEFAULT 'Funcionario',
  `situacao` INT NOT NULL,
  `id_oficina` INT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `IX_funcionario_cpf` (`cpf`),
  UNIQUE KEY `IX_funcionario_email` (`email`),
  KEY `IX_funcionario_id_oficina_nome` (`id_oficina`, `nome`),
  CONSTRAINT `CK_funcionario_situacao` CHECK (`situacao` IN (1, 2)),
  CONSTRAINT `FK_funcionario_oficina`
    FOREIGN KEY (`id_oficina`)
    REFERENCES `oficina` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `cliente_oficina` (
  `id_oficina` INT NOT NULL,
  `id_cliente` INT NOT NULL,
  `ativo` TINYINT(1) NOT NULL DEFAULT 1,
  `created_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `updated_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`id_oficina`, `id_cliente`),
  KEY `IX_cliente_oficina_id_cliente` (`id_cliente`),
  KEY `IX_cliente_oficina_oficina_ativo_cliente` (`id_oficina`, `ativo`, `id_cliente`),
  CONSTRAINT `FK_cliente_oficina_oficina`
    FOREIGN KEY (`id_oficina`)
    REFERENCES `oficina` (`id`)
    ON DELETE CASCADE
    ON UPDATE RESTRICT,
  CONSTRAINT `FK_cliente_oficina_cliente`
    FOREIGN KEY (`id_cliente`)
    REFERENCES `cliente` (`id`)
    ON DELETE CASCADE
    ON UPDATE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `veiculo` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nome` VARCHAR(100) NOT NULL,
  `tipo` VARCHAR(50) NOT NULL,
  `placa` VARCHAR(8) NOT NULL,
  `chassi` VARCHAR(17) NOT NULL,
  `ano` INT NOT NULL,
  `quilometragem` INT NOT NULL,
  `combustivel` VARCHAR(30) NOT NULL,
  `seguro` VARCHAR(100) NULL,
  `cor` TEXT NOT NULL,
  `status` INT NOT NULL,
  `id_cliente` INT NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `AK_veiculo_id_id_cliente` (`id`, `id_cliente`),
  KEY `IX_veiculo_id_cliente` (`id_cliente`),
  CONSTRAINT `CK_veiculo_status` CHECK (`status` IN (0, 1, 2, 3)),
  CONSTRAINT `FK_veiculo_cliente`
    FOREIGN KEY (`id_cliente`)
    REFERENCES `cliente` (`id`)
    ON DELETE CASCADE
    ON UPDATE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `marca` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nome` VARCHAR(100) NOT NULL,
  `desc` VARCHAR(500) NULL,
  `tipomarca` VARCHAR(50) NOT NULL,
  `VeiculoId` INT NULL,
  PRIMARY KEY (`id`),
  KEY `IX_marca_VeiculoId` (`VeiculoId`),
  CONSTRAINT `FK_marca_veiculo`
    FOREIGN KEY (`VeiculoId`)
    REFERENCES `veiculo` (`id`)
    ON DELETE SET NULL
    ON UPDATE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `peca` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nome` VARCHAR(100) NOT NULL,
  `tipo` VARCHAR(100) NOT NULL,
  `descricao` VARCHAR(500) NULL,
  `valor` DECIMAL(18, 2) NOT NULL,
  `quantidade` INT NOT NULL,
  `garantia` DATE NOT NULL,
  `unidade` INT NOT NULL,
  `idmarca` INT NOT NULL,
  `dataAquisicao` DATE NOT NULL,
  `fornecedor` VARCHAR(100) NOT NULL,
  `id_oficina` INT NULL,
  PRIMARY KEY (`id`),
  KEY `IX_peca_idmarca` (`idmarca`),
  KEY `IX_peca_id_oficina_nome` (`id_oficina`, `nome`),
  CONSTRAINT `FK_peca_marca`
    FOREIGN KEY (`idmarca`)
    REFERENCES `marca` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `FK_peca_oficina`
    FOREIGN KEY (`id_oficina`)
    REFERENCES `oficina` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `telefone` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `numero` VARCHAR(9) NOT NULL,
  `ddd` INT NOT NULL,
  `clienteid` INT NOT NULL,
  `FuncionarioId` INT NULL,
  `OficinaId` INT NULL,
  PRIMARY KEY (`id`),
  KEY `IX_telefone_clienteid` (`clienteid`),
  KEY `IX_telefone_FuncionarioId` (`FuncionarioId`),
  KEY `IX_telefone_OficinaId` (`OficinaId`),
  CONSTRAINT `FK_telefone_cliente`
    FOREIGN KEY (`clienteid`)
    REFERENCES `cliente` (`id`)
    ON DELETE CASCADE
    ON UPDATE RESTRICT,
  CONSTRAINT `FK_telefone_funcionario`
    FOREIGN KEY (`FuncionarioId`)
    REFERENCES `funcionario` (`id`)
    ON DELETE SET NULL
    ON UPDATE RESTRICT,
  CONSTRAINT `FK_telefone_oficina`
    FOREIGN KEY (`OficinaId`)
    REFERENCES `oficina` (`id`)
    ON DELETE SET NULL
    ON UPDATE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `compartilhamento_cliente` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `id_cliente` INT NOT NULL,
  `codigo_hash` VARCHAR(128) NOT NULL,
  `expira_em` DATETIME(6) NOT NULL,
  `usado_em` DATETIME(6) NULL,
  `ativo` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`),
  UNIQUE KEY `IX_compartilhamento_cliente_codigo_hash` (`codigo_hash`),
  KEY `IX_compartilhamento_cliente_id_cliente` (`id_cliente`),
  CONSTRAINT `FK_compartilhamento_cliente_cliente`
    FOREIGN KEY (`id_cliente`)
    REFERENCES `cliente` (`id`)
    ON DELETE CASCADE
    ON UPDATE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `compartilhamento_cliente_tentativa` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `id_oficina` INT NOT NULL,
  `codigo_hash` VARCHAR(128) NOT NULL,
  `ip_address` VARCHAR(64) NULL,
  `sucesso` TINYINT(1) NOT NULL,
  `motivo` VARCHAR(64) NOT NULL,
  `tentado_em` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`id`),
  KEY `IX_comp_tentativa_oficina_ip_data` (`id_oficina`, `ip_address`, `tentado_em`),
  CONSTRAINT `FK_comp_tentativa_oficina`
    FOREIGN KEY (`id_oficina`)
    REFERENCES `oficina` (`id`)
    ON DELETE CASCADE
    ON UPDATE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `funcionario_servico` (
  `idFuncionario` INT NOT NULL,
  `idServico` INT NOT NULL,
  `tempodec` TEXT NULL,
  PRIMARY KEY (`idFuncionario`, `idServico`),
  KEY `IX_funcionario_servico_idServico` (`idServico`),
  CONSTRAINT `FK_funcionario_servico_funcionario`
    FOREIGN KEY (`idFuncionario`)
    REFERENCES `funcionario` (`id`)
    ON DELETE CASCADE
    ON UPDATE RESTRICT,
  CONSTRAINT `FK_funcionario_servico_servico`
    FOREIGN KEY (`idServico`)
    REFERENCES `servico` (`id`)
    ON DELETE CASCADE
    ON UPDATE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `registro_servico` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `id_veiculo` INT NOT NULL,
  `id_servico` INT NULL,
  `data_servico` DATETIME(6) NOT NULL,
  `descricao` TEXT NULL,
  `quilometragem` INT NOT NULL,
  `responsavel` TEXT NULL,
  PRIMARY KEY (`id`),
  KEY `IX_registro_servico_id_servico` (`id_servico`),
  KEY `IX_registro_servico_veiculo_data` (`id_veiculo`, `data_servico` DESC),
  CONSTRAINT `FK_registro_servico_veiculo`
    FOREIGN KEY (`id_veiculo`)
    REFERENCES `veiculo` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `FK_registro_servico_servico`
    FOREIGN KEY (`id_servico`)
    REFERENCES `servico` (`id`)
    ON DELETE SET NULL
    ON UPDATE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `peca_substituida` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `id_registro_servico` INT NOT NULL,
  `nome` TEXT NULL,
  `quantidade` INT NOT NULL,
  `observacao` TEXT NULL,
  PRIMARY KEY (`id`),
  KEY `IX_peca_substituida_id_registro_servico` (`id_registro_servico`),
  CONSTRAINT `FK_peca_substituida_registro_servico`
    FOREIGN KEY (`id_registro_servico`)
    REFERENCES `registro_servico` (`id`)
    ON DELETE CASCADE
    ON UPDATE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `pedido` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `id_cliente` INT NOT NULL,
  `id_funcionario` INT NOT NULL,
  `id_oficina` INT NOT NULL,
  `id_veiculo` INT NOT NULL,
  `valorTotal` DECIMAL(18, 2) NOT NULL,
  `descontoReais` DECIMAL(18, 2) NOT NULL,
  `descontoPorcentagem` DECIMAL(5, 2) NOT NULL,
  `descontoTotalReais` DECIMAL(18, 2) NOT NULL,
  `descontoServicoPorcentagem` DECIMAL(5, 2) NOT NULL,
  `descontoServicoReais` DECIMAL(18, 2) NOT NULL,
  `descontoPecaPorcentagem` DECIMAL(5, 2) NOT NULL,
  `descontoPecaReais` DECIMAL(18, 2) NOT NULL,
  `observacao` VARCHAR(500) NULL,
  `dataInicio` DATE NOT NULL,
  `dataFim` DATE NOT NULL,
  PRIMARY KEY (`id`),
  KEY `IX_pedido_id_cliente` (`id_cliente`),
  KEY `IX_pedido_id_funcionario` (`id_funcionario`),
  KEY `IX_pedido_id_oficina_id_cliente` (`id_oficina`, `id_cliente`),
  KEY `IX_pedido_id_veiculo_id_cliente` (`id_veiculo`, `id_cliente`),
  KEY `IX_pedido_id_veiculo_dataInicio` (`id_veiculo`, `dataInicio` DESC),
  CONSTRAINT `FK_pedido_cliente`
    FOREIGN KEY (`id_cliente`)
    REFERENCES `cliente` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `FK_pedido_funcionario`
    FOREIGN KEY (`id_funcionario`)
    REFERENCES `funcionario` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `FK_pedido_oficina`
    FOREIGN KEY (`id_oficina`)
    REFERENCES `oficina` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `FK_pedido_cliente_oficina`
    FOREIGN KEY (`id_oficina`, `id_cliente`)
    REFERENCES `cliente_oficina` (`id_oficina`, `id_cliente`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT,
  CONSTRAINT `FK_pedido_veiculo_cliente`
    FOREIGN KEY (`id_veiculo`, `id_cliente`)
    REFERENCES `veiculo` (`id`, `id_cliente`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `pedido_peca` (
  `idpedido` INT NOT NULL,
  `idpeca` INT NOT NULL,
  `quantidade` INT NOT NULL,
  `datainstalacao` DATE NOT NULL,
  `estado` TEXT NULL,
  `observacao` TEXT NULL,
  PRIMARY KEY (`idpedido`, `idpeca`),
  KEY `IX_pedido_peca_idpeca` (`idpeca`),
  CONSTRAINT `FK_pedido_peca_pedido`
    FOREIGN KEY (`idpedido`)
    REFERENCES `pedido` (`id`)
    ON DELETE CASCADE
    ON UPDATE RESTRICT,
  CONSTRAINT `FK_pedido_peca_peca`
    FOREIGN KEY (`idpeca`)
    REFERENCES `peca` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `pedido_servico` (
  `idPedido` INT NOT NULL,
  `idServico` INT NOT NULL,
  `quantVezes` INT NOT NULL,
  PRIMARY KEY (`idPedido`, `idServico`),
  KEY `IX_pedido_servico_idServico` (`idServico`),
  CONSTRAINT `FK_pedido_servico_pedido`
    FOREIGN KEY (`idPedido`)
    REFERENCES `pedido` (`id`)
    ON DELETE CASCADE
    ON UPDATE RESTRICT,
  CONSTRAINT `FK_pedido_servico_servico`
    FOREIGN KEY (`idServico`)
    REFERENCES `servico` (`id`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `veiculo_imagem` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `id_veiculo` INT NOT NULL,
  `url` VARCHAR(300) NOT NULL,
  `nome_arquivo` VARCHAR(150) NOT NULL,
  `nome_original` VARCHAR(255) NOT NULL,
  `content_type` VARCHAR(100) NOT NULL,
  `tamanho_bytes` BIGINT NOT NULL,
  `criado_em` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`id`),
  UNIQUE KEY `IX_veiculo_imagem_nome_arquivo` (`nome_arquivo`),
  KEY `IX_veiculo_imagem_veiculo_criado_em` (`id_veiculo`, `criado_em`),
  CONSTRAINT `FK_veiculo_imagem_veiculo`
    FOREIGN KEY (`id_veiculo`)
    REFERENCES `veiculo` (`id`)
    ON DELETE CASCADE
    ON UPDATE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET SQL_MODE = @OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS = @OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS = @OLD_UNIQUE_CHECKS;
