namespace SIGO.Objects.Dtos.Entities;

public sealed record HistoricoVeiculoDTO(
    HistoricoClienteDTO Cliente,
    HistoricoVeiculoResumoDTO Veiculo,
    IReadOnlyList<HistoricoVeiculoEventoDTO> Eventos);

public sealed record HistoricoClienteDTO(
    int Id,
    string Nome,
    string? Email,
    string? Documento);

public sealed record HistoricoVeiculoResumoDTO(
    int Id,
    int ClienteId,
    string Nome,
    string Tipo,
    string Placa,
    string Chassi,
    int AnoFabricacao,
    int QuilometragemAtual,
    string Combustivel,
    string? Seguro,
    string Cor);

public sealed record HistoricoVeiculoEventoDTO(
    string Origem,
    int EventoId,
    int? PedidoId,
    DateTime DataInicio,
    DateTime? DataFim,
    int OficinaId,
    string OficinaNome,
    int? FuncionarioId,
    string? FuncionarioNome,
    string? Descricao,
    int? Quilometragem,
    decimal? ValorBruto,
    decimal? ValorTotal,
    decimal? DescontoTotal,
    IReadOnlyList<HistoricoServicoDTO> Servicos,
    IReadOnlyList<HistoricoPecaDTO> Pecas);

public sealed record HistoricoServicoDTO(
    int? Id,
    string Nome,
    string? Descricao,
    int Quantidade,
    decimal? Valor);

public sealed record HistoricoPecaDTO(
    int? Id,
    string Nome,
    int Quantidade,
    DateOnly? DataInstalacao,
    string? Estado,
    string? Observacao,
    decimal? ValorUnitario);
