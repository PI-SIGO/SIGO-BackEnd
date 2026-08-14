using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SIGO.Data;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Enums;
using SIGO.Objects.Models;
using SIGO.Security;
using SIGO.Services.Interfaces;
using SIGO.Validation;

namespace SIGO.Services.Entities;

public class ReportService : IReportService
{
    private const string PedidoOrigin = "Pedido";
    private const string ServiceRecordOrigin = "Registro de servico";

    private readonly IRegistroServicoRepository _registroRepo;
    private readonly IPedidoRepository _pedidoRepo;
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ReportService(
        IRegistroServicoRepository registroRepo,
        IPedidoRepository pedidoRepo,
        AppDbContext context,
        ICurrentUserService currentUserService)
    {
        _registroRepo = registroRepo;
        _pedidoRepo = pedidoRepo;
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<HistoricoVeiculoDTO> GetVehicleHistoryAsync(
        int veiculoId,
        DateTime? from = null,
        DateTime? to = null,
        string? tipoServico = null,
        CancellationToken cancellationToken = default)
    {
        ValidateDateRange(from, to);

        var tipoNormalizado = string.IsNullOrWhiteSpace(tipoServico)
            ? null
            : tipoServico.Trim();
        var toInclusive = NormalizeInclusiveEnd(to);

        var vehicle = await _context.Veiculos
            .AsNoTracking()
            .Include(v => v.Cliente)
            .FirstOrDefaultAsync(v => v.Id == veiculoId, cancellationToken)
            ?? throw new KeyNotFoundException("Veiculo nao encontrado.");

        var registros = (await _registroRepo.GetByVeiculoAsync(
                veiculoId,
                from,
                toInclusive,
                tipoNormalizado,
                cancellationToken))
            .ToList();
        var pedidos = FilterPedidos(
                await _pedidoRepo.GetByVeiculoWithDetailsAsync(veiculoId, cancellationToken),
                from,
                to,
                tipoNormalizado)
            .ToList();

        if ((_currentUserService.IsInRole(SystemRoles.Oficina) ||
             _currentUserService.IsInRole(SystemRoles.Funcionario)) &&
            _currentUserService.OficinaId.HasValue)
        {
            var oficinaId = _currentUserService.OficinaId.Value;
            registros = registros.Where(r => r.OficinaId == oficinaId).ToList();
            pedidos = pedidos.Where(p => p.idOficina == oficinaId).ToList();
        }

        var events = registros
            .Select(MapServiceRecord)
            .Concat(pedidos.Select(MapOrder))
            .OrderByDescending(item => item.DataInicio)
            .ThenByDescending(item => item.EventoId)
            .ToArray();

        var includeClientDocument = await CanIncludeClientDocumentAsync(
            vehicle,
            cancellationToken);

        return new HistoricoVeiculoDTO(
            new HistoricoClienteDTO(
                vehicle.ClienteId,
                vehicle.Cliente?.Nome ?? "Cliente nao disponivel",
                vehicle.Cliente?.Email,
                includeClientDocument ? vehicle.Cliente?.Cpf_Cnpj : null),
            new HistoricoVeiculoResumoDTO(
                vehicle.Id,
                vehicle.ClienteId,
                vehicle.NomeVeiculo,
                vehicle.ModeloVeiculo,
                vehicle.PlacaVeiculo,
                vehicle.ChassiVeiculo,
                vehicle.AnoFab,
                vehicle.Quilometragem,
                vehicle.Combustivel,
                vehicle.Seguro,
                vehicle.Cor),
            events);
    }

    public async Task<byte[]> GenerateVehicleHistoryPdfAsync(
        int veiculoId,
        DateTime? from = null,
        DateTime? to = null,
        string? tipoServico = null,
        CancellationToken cancellationToken = default)
    {
        var history = await GetVehicleHistoryAsync(
            veiculoId,
            from,
            to,
            tipoServico,
            cancellationToken);

        return Document
            .Create(container => new VehicleHistoryDocument(history).Compose(container))
            .GeneratePdf();
    }

    public async Task<byte[]> GenerateVehicleHistoryExcelAsync(
        int veiculoId,
        DateTime? from = null,
        DateTime? to = null,
        string? tipoServico = null,
        CancellationToken cancellationToken = default)
    {
        var history = await GetVehicleHistoryAsync(
            veiculoId,
            from,
            to,
            tipoServico,
            cancellationToken);

        using var workbook = new XLWorkbook();
        CreateSummaryWorksheet(workbook, history);
        CreateHistoryWorksheet(workbook, history.Eventos);

        await using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static IEnumerable<Pedido> FilterPedidos(
        IEnumerable<Pedido> pedidos,
        DateTime? from,
        DateTime? to,
        string? tipoServico)
    {
        ArgumentNullException.ThrowIfNull(pedidos);
        ValidateDateRange(from, to);

        var query = pedidos;
        if (from.HasValue)
        {
            var fromDate = DateOnly.FromDateTime(from.Value);
            query = query.Where(p => p.DataInicio >= fromDate);
        }

        if (to.HasValue)
        {
            var toDate = DateOnly.FromDateTime(to.Value);
            query = query.Where(p => p.DataInicio <= toDate);
        }

        if (!string.IsNullOrWhiteSpace(tipoServico))
        {
            var tipoNormalizado = tipoServico.Trim();
            query = query.Where(p => p.Pedido_Servicos?.Any(ps =>
                ps.Servico?.Nome?.Contains(
                    tipoNormalizado,
                    StringComparison.OrdinalIgnoreCase) == true) == true);
        }

        return query;
    }

    public async Task<bool> CanAccessVehicleHistoryAsync(
        int veiculoId,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService.IsInRole(SystemRoles.Admin))
        {
            return await _context.Veiculos.AnyAsync(
                v => v.Id == veiculoId,
                cancellationToken);
        }

        if (_currentUserService.IsInRole(SystemRoles.Cliente))
        {
            var clienteId = _currentUserService.UserId;
            return clienteId.HasValue &&
                   await _context.Veiculos.AnyAsync(
                       v => v.Id == veiculoId && v.ClienteId == clienteId.Value,
                       cancellationToken);
        }

        if (_currentUserService.IsInRole(SystemRoles.Oficina) ||
            _currentUserService.IsInRole(SystemRoles.Funcionario))
        {
            var oficinaId = _currentUserService.OficinaId;
            if (!oficinaId.HasValue)
                return false;

            return await _context.Veiculos.AnyAsync(v =>
                    v.Id == veiculoId &&
                    v.Cliente.Situacao == Situacao.ATIVO &&
                    v.Cliente.ClienteOficinas.Any(co =>
                        co.OficinaId == oficinaId.Value && co.Ativo),
                cancellationToken);
        }

        return false;
    }

    private static HistoricoVeiculoEventoDTO MapServiceRecord(RegistroServico record)
    {
        var services = record.Servico is null
            ? Array.Empty<HistoricoServicoDTO>()
            : new[]
            {
                new HistoricoServicoDTO(
                    record.Servico.Id,
                    record.Servico.Nome,
                    record.Servico.Descricao,
                    1,
                    record.Servico.Valor)
            };

        var pieces = record.PecasSubstituidas?
            .Select(piece => new HistoricoPecaDTO(
                null,
                piece.Nome,
                piece.Quantidade,
                null,
                "Substituida",
                piece.Observacao,
                null))
            .ToArray() ?? Array.Empty<HistoricoPecaDTO>();

        return new HistoricoVeiculoEventoDTO(
            ServiceRecordOrigin,
            record.Id,
            null,
            record.DataServico,
            null,
            record.OficinaId,
            record.Oficina?.Nome ?? "Oficina nao disponivel",
            null,
            NullIfWhiteSpace(record.Responsavel),
            NullIfWhiteSpace(record.Descricao),
            record.Quilometragem > 0 ? record.Quilometragem : null,
            null,
            null,
            null,
            services,
            pieces);
    }

    private static HistoricoVeiculoEventoDTO MapOrder(Pedido order)
    {
        var services = order.Pedido_Servicos?
            .Select(item => new HistoricoServicoDTO(
                item.IdServico,
                item.Servico?.Nome ?? "Servico nao disponivel",
                item.Servico?.Descricao,
                item.QuantVezes,
                item.ValorUnitario))
            .ToArray() ?? Array.Empty<HistoricoServicoDTO>();

        var pieces = order.Pedido_Pecas?
            .Select(item => new HistoricoPecaDTO(
                item.IdPeca,
                item.Peca?.Nome ?? "Peca nao disponivel",
                item.Quantidade,
                item.DataInstalacao,
                NullIfWhiteSpace(item.Estado),
                NullIfWhiteSpace(item.Observacao),
                item.ValorUnitario))
            .ToArray() ?? Array.Empty<HistoricoPecaDTO>();

        var grossTotal = services.Sum(item => (item.Valor ?? 0m) * item.Quantidade) +
                         pieces.Sum(item => (item.ValorUnitario ?? 0m) * item.Quantidade);

        return new HistoricoVeiculoEventoDTO(
            PedidoOrigin,
            order.Id,
            order.Id,
            order.DataInicio.ToDateTime(TimeOnly.MinValue),
            order.DataFim.ToDateTime(TimeOnly.MinValue),
            order.idOficina,
            order.Oficina?.Nome ?? "Oficina nao disponivel",
            order.idFuncionario > 0 ? order.idFuncionario : null,
            order.Funcionario?.Nome,
            NullIfWhiteSpace(order.Observacao),
            null,
            grossTotal,
            order.ValorTotal,
            order.DescontoTotalReais,
            services,
            pieces);
    }

    private async Task<bool> CanIncludeClientDocumentAsync(
        Veiculo vehicle,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.IsInRole(SystemRoles.Admin))
            return true;

        if (_currentUserService.IsInRole(SystemRoles.Cliente))
            return _currentUserService.UserId == vehicle.ClienteId;

        var oficinaId = _currentUserService.OficinaId;
        if (!oficinaId.HasValue)
            return false;

        return await _context.ClienteOficinas.AnyAsync(co =>
                co.ClienteId == vehicle.ClienteId &&
                co.OficinaId == oficinaId.Value &&
                co.Ativo &&
                co.Cliente.Situacao == Situacao.ATIVO,
            cancellationToken);
    }

    private static void ValidateDateRange(DateTime? from, DateTime? to)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            throw new BusinessValidationException(new[]
            {
                new ValidationError("from", "A data inicial nao pode ser posterior a data final.")
            });
        }
    }

    private static DateTime? NormalizeInclusiveEnd(DateTime? value)
    {
        if (!value.HasValue || value.Value.TimeOfDay != TimeSpan.Zero)
            return value;

        return value.Value.Date.AddDays(1).AddTicks(-1);
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FormatServices(IEnumerable<HistoricoServicoDTO> services) =>
        string.Join(", ", services.Select(item =>
            item.Quantidade > 1 ? $"{item.Nome} x{item.Quantidade}" : item.Nome));

    private static string FormatPieces(IEnumerable<HistoricoPecaDTO> pieces) =>
        string.Join(", ", pieces.Select(item => $"{item.Nome} x{item.Quantidade}"));

    private static void CreateSummaryWorksheet(
        XLWorkbook workbook,
        HistoricoVeiculoDTO history)
    {
        var sheet = workbook.Worksheets.Add("Resumo");
        var rows = new (string Label, object? Value)[]
        {
            ("Cliente", history.Cliente.Nome),
            ("E-mail", history.Cliente.Email),
            ("CPF/CNPJ", history.Cliente.Documento),
            ("Veiculo", history.Veiculo.Nome),
            ("Tipo", history.Veiculo.Tipo),
            ("Placa", history.Veiculo.Placa),
            ("Chassi", history.Veiculo.Chassi),
            ("Ano", history.Veiculo.AnoFabricacao),
            ("Quilometragem atual", history.Veiculo.QuilometragemAtual),
            ("Combustivel", history.Veiculo.Combustivel),
            ("Seguro", history.Veiculo.Seguro),
            ("Cor", history.Veiculo.Cor),
            ("Total de eventos", history.Eventos.Count)
        };

        sheet.Cell(1, 1).Value = "Historico completo do veiculo";
        sheet.Range(1, 1, 1, 2).Merge();
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 16;
        sheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.DarkBlue;
        sheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;

        for (var index = 0; index < rows.Length; index++)
        {
            var row = index + 3;
            sheet.Cell(row, 1).Value = rows[index].Label;
            sheet.Cell(row, 1).Style.Font.Bold = true;
            sheet.Cell(row, 2).Value = rows[index].Value?.ToString() ?? string.Empty;
        }

        sheet.Columns().AdjustToContents();
    }

    /*para estilizar o excel modifique aqui*/
    private static void CreateHistoryWorksheet(
        XLWorkbook workbook,
        IReadOnlyList<HistoricoVeiculoEventoDTO> events)
    {
        var sheet = workbook.Worksheets.Add("Historico");
        var headers = new[]
        {
            "Origem", "Data inicial", "Data final", "Oficina", "Funcionario",
            "Servicos", "Pecas", "Descricao", "Quilometragem",
            "Valor bruto", "Desconto total", "Valor total"
        };

        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
            sheet.Cell(1, column + 1).Style.Font.Bold = true;
            sheet.Cell(1, column + 1).Style.Fill.BackgroundColor = XLColor.DarkBlue;
            sheet.Cell(1, column + 1).Style.Font.FontColor = XLColor.White;
        }

        for (var index = 0; index < events.Count; index++)
        {
            var item = events[index];
            var row = index + 2;
            sheet.Cell(row, 1).Value = item.Origem;
            sheet.Cell(row, 2).Value = item.DataInicio;
            sheet.Cell(row, 3).Value = item.DataFim?.ToString("dd/MM/yyyy") ?? string.Empty;
            sheet.Cell(row, 4).Value = item.OficinaNome;
            sheet.Cell(row, 5).Value = item.FuncionarioNome ?? string.Empty;
            sheet.Cell(row, 6).Value = FormatServices(item.Servicos);
            sheet.Cell(row, 7).Value = FormatPieces(item.Pecas);
            sheet.Cell(row, 8).Value = item.Descricao ?? string.Empty;
            sheet.Cell(row, 9).Value = item.Quilometragem?.ToString() ?? string.Empty;
            sheet.Cell(row, 10).Value = item.ValorBruto?.ToString("F2") ?? string.Empty;
            sheet.Cell(row, 11).Value = item.DescontoTotal?.ToString("F2") ?? string.Empty;
            sheet.Cell(row, 12).Value = item.ValorTotal?.ToString("F2") ?? string.Empty;
        }

        if (events.Count > 0)
            sheet.Range(1, 1, events.Count + 1, headers.Length).CreateTable();

        sheet.SheetView.FreezeRows(1);
        sheet.CellsUsed().Style.Alignment.WrapText = true;
        sheet.Columns().AdjustToContents();
        foreach (var column in sheet.ColumnsUsed())
        {
            if (column.Width > 60)
                column.Width = 60;
        }
    }

    /*Para estilizar o pdf modifique aqui*/
    private sealed class VehicleHistoryDocument
    {
        private readonly HistoricoVeiculoDTO _history;

        public VehicleHistoryDocument(HistoricoVeiculoDTO history)
        {
            _history = history;
        }

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Text("Relatorio de Historico do Veiculo")
                    .SemiBold()
                    .FontSize(20);

                page.Content().Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Element(ComposeSummary);
                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    if (_history.Eventos.Count == 0)
                    {
                        column.Item().Text("Nenhum registro encontrado.");
                        return;
                    }

                    foreach (var item in _history.Eventos)
                        column.Item().Element(container => ComposeEvent(container, item));
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("SIGO - Sistema de Gestao | ");
                    text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                });
            });
        }

        private void ComposeSummary(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Dados do cliente").Bold();
                    column.Item().Text($"Nome: {_history.Cliente.Nome}");
                    column.Item().Text($"E-mail: {_history.Cliente.Email ?? "-"}");
                    if (!string.IsNullOrWhiteSpace(_history.Cliente.Documento))
                        column.Item().Text($"CPF/CNPJ: {_history.Cliente.Documento}");
                });

                row.ConstantItem(20);

                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Dados do veiculo").Bold();
                    column.Item().Text($"Modelo: {_history.Veiculo.Nome}");
                    column.Item().Text($"Placa: {_history.Veiculo.Placa}");
                    column.Item().Text($"Chassi: {_history.Veiculo.Chassi}");
                    column.Item().Text($"Ano: {_history.Veiculo.AnoFabricacao}");
                    column.Item().Text($"Quilometragem atual: {_history.Veiculo.QuilometragemAtual}");
                });
            });
        }

        private static void ComposeEvent(
            IContainer container,
            HistoricoVeiculoEventoDTO item)
        {
            container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(8)
                .Column(column =>
                {
                    column.Spacing(3);
                    column.Item().Text(item.Origem).Bold();
                    column.Item().Text(
                        $"Periodo: {item.DataInicio:dd/MM/yyyy}" +
                        (item.DataFim.HasValue ? $" a {item.DataFim:dd/MM/yyyy}" : string.Empty));
                    column.Item().Text($"Oficina: {item.OficinaNome}");
                    column.Item().Text($"Funcionario: {item.FuncionarioNome ?? "Nao informado"}");
                    column.Item().Text($"Servicos: {FormatServices(item.Servicos).DefaultIfEmpty("-")}");
                    column.Item().Text($"Pecas: {FormatPieces(item.Pecas).DefaultIfEmpty("-")}");
                    column.Item().Text($"Descricao: {item.Descricao ?? "-"}");
                    column.Item().Text($"Quilometragem: {item.Quilometragem?.ToString() ?? "-"}");

                    if (item.ValorTotal.HasValue)
                    {
                        column.Item().Text(
                            $"Valor bruto: {(item.ValorBruto ?? 0):C2} | " +
                            $"Desconto: {(item.DescontoTotal ?? 0):C2} | " +
                            $"Valor total: {item.ValorTotal.Value:C2}");
                    }
                });
        }
    }
}

internal static class ReportStringExtensions
{
    public static string DefaultIfEmpty(this string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;
}
