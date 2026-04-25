using QuestPDF.Fluent;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SIGO.Data.Interfaces;
using SIGO.Data;
using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Models;
using SIGO.Services.Interfaces;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace SIGO.Services.Entities
{
    public class ReportService : IReportService
    {
        private readonly IRegistroServicoRepository _registroRepo;
        private readonly IPedidoRepository _pedidoRepo;
        private readonly AppDbContext _context;

        public ReportService(IRegistroServicoRepository registroRepo, IPedidoRepository pedidoRepo, AppDbContext context)
        {
            _registroRepo = registroRepo;
            _pedidoRepo = pedidoRepo;
            _context = context;
        }

        public async Task<byte[]> GenerateVehicleHistoryPdfAsync(int veiculoId, DateTime? from = null, DateTime? to = null, string? tipoServico = null)
        {
            var registros = (await _registroRepo.GetByVeiculoAsync(veiculoId, from, to, tipoServico)).ToList();
            var pedidos = (await _pedidoRepo.GetByVeiculoWithDetailsAsync(veiculoId)).ToList();

            // Map registros and pedidos to unified entries
            var entries = new List<ReportEntry>();

            // Map registros and try to resolve responsible funcionario ids when not present
            foreach (var r in registros)
            {
                string responsavel = r.Responsavel;

                if (string.IsNullOrWhiteSpace(responsavel) && r.ServicoId.HasValue)
                {
                    var funcIds = await _context.Set<Funcionario_Servico>()
                        .Where(fs => fs.IdServico == r.ServicoId.Value)
                        .Select(fs => fs.IdFuncionario)
                        .Distinct()
                        .ToListAsync();

                    if (funcIds != null && funcIds.Any())
                        responsavel = string.Join(", ", funcIds);
                }

                entries.Add(new ReportEntry
                {
                    Date = r.DataServico,
                    Tipo = r.Servico?.Nome ?? "-",
                    Descricao = (r.Descricao ?? "") + (r.PecasSubstituidas != null && r.PecasSubstituidas.Any()
                        ? "\nPeças: " + string.Join(", ", r.PecasSubstituidas.Select(p => $"{p.Nome} (x{p.Quantidade})"))
                        : ""),
                    Quilometragem = r.Quilometragem > 0 ? (int?)r.Quilometragem : null,
                    Responsavel = responsavel
                });
            }

            entries.AddRange(pedidos.Select(p => new ReportEntry
            {
                Date = p.DataInicio.ToDateTime(TimeOnly.MinValue),
                Tipo = p.Pedido_Servicos != null && p.Pedido_Servicos.Any()
                    ? string.Join(", ", p.Pedido_Servicos.Select(ps => ps.Servico?.Nome + (ps.QuantVezes > 1 ? $" x{ps.QuantVezes}" : "")))
                    : "-",
                Descricao = (p.Observacao ?? "") + (p.Pedido_Pecas != null && p.Pedido_Pecas.Any()
                    ? "\nPeças: " + string.Join(", ", p.Pedido_Pecas.Select(pp => $"{pp.Peca?.Nome} (x{pp.Quantidade})"))
                    : ""),
                Quilometragem = null,
                Responsavel = p.Oficina?.Nome ?? "-"
            }));

            var combined = entries.OrderByDescending(e => e.Date).ToList();

            // Determine vehicle and client info
            var vehicle = registros.FirstOrDefault()?.Veiculo ?? pedidos.FirstOrDefault()?.Veiculo;
            var client = vehicle?.Cliente ?? pedidos.FirstOrDefault()?.Cliente ?? registros.FirstOrDefault()?.Veiculo?.Cliente;

            var doc = new VehicleHistoryDocument(combined, vehicle, client);
            byte[] pdf = Document.Create(container => doc.Compose(container)).GeneratePdf();
            return pdf;
        }

        private class ReportEntry
        {
            public DateTime Date { get; set; }
            public string Tipo { get; set; }
            public string Descricao { get; set; }
            public int? Quilometragem { get; set; }
            public string Responsavel { get; set; }
        }

        private class VehicleHistoryDocument : IDocument
        {
            private readonly List<ReportEntry> _entries;
            private readonly Veiculo _vehicle;
            private readonly Cliente _client;

            public VehicleHistoryDocument(List<ReportEntry> entries, Veiculo vehicle, Cliente client)
            {
                _entries = entries ?? new List<ReportEntry>();
                _vehicle = vehicle;
                _client = client;
            }

            public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

            public void Compose(IDocumentContainer container)
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Text("Relatório de Histórico do Veículo").SemiBold().FontSize(20);

                    page.Content().Column(column =>
                    {
                        column.Item().Element(ComposeVehicleAndClient);
                        column.Item().PaddingTop(10).LineHorizontal(1);
                        column.Item().Element(ComposeTable);
                        column.Item().PaddingTop(10).AlignRight().Text($"Gerado em: {DateTime.Now:yyyy-MM-dd HH:mm}");
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("SIGO - Sistema de Gestão");
                    });
                });
            }

            void ComposeVehicleAndClient(IContainer container)
            {
                if (!_entries.Any())
                {
                    container.Text("Nenhum registro encontrado.").FontSize(12);
                    return;
                }

                var cliente = _client;

                container.Row(row =>
                {
                    row.RelativeColumn().Column(col =>
                    {
                        col.Item().Text("Dados do Cliente").Bold();
                        if (cliente != null)
                        {
                            col.Item().Text($"Nome: {cliente.Nome}");
                            col.Item().Text($"Email: {cliente.Email}");
                            col.Item().Text($"CPF/CNPJ: {cliente.Cpf_Cnpj}");
                        }
                        else
                        {
                            col.Item().Text("Cliente não disponível");
                        }
                    });

                    row.ConstantColumn(20);

                    row.RelativeColumn().Column(col =>
                    {
                        col.Item().Text("Dados do Veículo").Bold();
                        var v = _vehicle;
                        if (v != null)
                        {
                            col.Item().Text($"Modelo: {v.NomeVeiculo}");
                            col.Item().Text($"Placa: {v.PlacaVeiculo}");
                            col.Item().Text($"Ano: {v.AnoFab}");
                            col.Item().Text($"Quilometragem atual: {v.Quilometragem}");
                        }
                        else
                        {
                            col.Item().Text("Veículo não disponível");
                        }
                    });
                });
            }

            void ComposeTable(IContainer container)
            {
                container.Table(table =>
                {
                    // columns
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2); // data
                        columns.RelativeColumn(3); // tipo
                        columns.RelativeColumn(4); // descricao
                        columns.RelativeColumn(2); // km
                        columns.RelativeColumn(2); // responsavel
                    });

                    // header
                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Data");
                        header.Cell().Element(CellStyle).Text("Tipo de Serviço");
                        header.Cell().Element(CellStyle).Text("Descrição / Peças");
                        header.Cell().Element(CellStyle).Text("Quilometragem");
                        header.Cell().Element(CellStyle).Text("Responsável");

                        static IContainer CellStyle(IContainer c) => c.DefaultTextStyle(x => x.SemiBold()).Padding(5).Background(Colors.Grey.Lighten3);
                    });

                    foreach (var r in _entries)
                    {
                        table.Cell().Element(CellBody).Text(r.Date.ToString("yyyy-MM-dd"));
                        table.Cell().Element(CellBody).Text(r.Tipo ?? "-");

                        var descricao = r.Descricao ?? "";
                        table.Cell().Element(CellBody).Text(descricao);

                        table.Cell().Element(CellBody).AlignRight().Text(r.Quilometragem.HasValue ? r.Quilometragem.Value.ToString() : "-");
                        table.Cell().Element(CellBody).Text(r.Responsavel ?? "-");

                        static IContainer CellBody(IContainer c) => c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).MinHeight(20);
                    }
                });
            }
        }
    }
}
