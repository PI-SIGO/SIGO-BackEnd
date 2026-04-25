-- Tabelas para histórico de serviços de veículos

CREATE TABLE registro_servico (
    id SERIAL PRIMARY KEY,
    id_veiculo INTEGER NOT NULL REFERENCES veiculo(id),
    id_servico INTEGER NULL REFERENCES servico(id),
    data_servico TIMESTAMP NOT NULL,
    descricao TEXT,
    quilometragem INTEGER,
    responsavel VARCHAR(255)
);

CREATE TABLE peca_substituida (
    id SERIAL PRIMARY KEY,
    id_registro_servico INTEGER NOT NULL REFERENCES registro_servico(id),
    nome VARCHAR(255) NOT NULL,
    quantidade INTEGER DEFAULT 1,
    observacao TEXT
);

-- Consulta para obter histórico completo de um veículo (ordenado por data descendente)
-- Parâmetros: @veiculoId, @from (opcional), @to (opcional), @tipoServico (opcional)

-- Exemplo de query:
-- SELECT rs.*, s.nome as tipo_servico, p.nome as peca, p.quantidade
-- FROM registro_servico rs
-- LEFT JOIN servico s ON s.id = rs.id_servico
-- LEFT JOIN peca_substituida p ON p.id_registro_servico = rs.id
-- WHERE rs.id_veiculo = @veiculoId
--   AND (@from IS NULL OR rs.data_servico >= @from)
--   AND (@to IS NULL OR rs.data_servico <= @to)
--   AND (@tipoServico IS NULL OR s.nome ILIKE '%' || @tipoServico || '%')
-- ORDER BY rs.data_servico DESC;
