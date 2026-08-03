using Microsoft.AspNetCore.Mvc;
using Moq;
using SIGO.Controllers;
using SIGO.Objects.Dtos.Entities;
using SIGO.Services.Interfaces;
using Xunit;

namespace SIGO.Tests.Controllers;

public class ReportControllerSecurityTests
{
    [Fact]
    public async Task GetVehicleHistoryPdf_DeveRetornarForbid_QuandoUsuarioNaoTemAcessoAoVeiculo()
    {
        var reportService = new Mock<IReportService>();
        reportService
            .Setup(service => service.CanAccessVehicleHistoryAsync(
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = new ReportController(reportService.Object);

        var result = await controller.GetVehicleHistoryPdf(10);

        Assert.IsType<ForbidResult>(result);
        reportService.Verify(service => service.GenerateVehicleHistoryPdfAsync(
            It.IsAny<int>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetVehicleHistoryPdf_DeveGerarPdf_QuandoUsuarioTemAcessoAoVeiculo()
    {
        var reportService = CreateAuthorizedService();
        reportService
            .Setup(service => service.GenerateVehicleHistoryPdfAsync(
                10,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 1, 2, 3 });
        var controller = new ReportController(reportService.Object);

        var result = await controller.GetVehicleHistoryPdf(10);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.EndsWith(".pdf", file.FileDownloadName);
    }

    [Fact]
    public async Task GetVehicleHistoryPdf_DeveEncaminharTodosOsFiltros()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 1, 31);
        var reportService = CreateAuthorizedService();
        reportService
            .Setup(service => service.GenerateVehicleHistoryPdfAsync(
                10,
                from,
                to,
                "revisao",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 1 });
        var controller = new ReportController(reportService.Object);

        var result = await controller.GetVehicleHistoryPdf(10, from, to, "revisao");

        Assert.IsType<FileContentResult>(result);
        reportService.Verify(service => service.GenerateVehicleHistoryPdfAsync(
            10,
            from,
            to,
            "revisao",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetVehicleHistory_DeveRetornarHistoricoEstruturado()
    {
        var history = CreateHistory();
        var reportService = CreateAuthorizedService();
        reportService
            .Setup(service => service.GetVehicleHistoryAsync(
                10,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);
        var controller = new ReportController(reportService.Object);

        var result = await controller.GetVehicleHistory(10);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(history, ok.Value);
    }

    [Fact]
    public async Task GetVehicleHistoryExcel_DeveRetornarArquivoXlsx()
    {
        var reportService = CreateAuthorizedService();
        reportService
            .Setup(service => service.GenerateVehicleHistoryExcelAsync(
                10,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 1, 2, 3 });
        var controller = new ReportController(reportService.Object);

        var result = await controller.GetVehicleHistoryExcel(10);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            file.ContentType);
        Assert.EndsWith(".xlsx", file.FileDownloadName);
    }

    private static Mock<IReportService> CreateAuthorizedService()
    {
        var service = new Mock<IReportService>();
        service
            .Setup(item => item.CanAccessVehicleHistoryAsync(
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return service;
    }

    private static HistoricoVeiculoDTO CreateHistory() => new(
        new HistoricoClienteDTO(5, "Cliente", "cliente@example.com", null),
        new HistoricoVeiculoResumoDTO(
            10,
            5,
            "Veiculo",
            "Carro",
            "ABC1D23",
            "12345678901234567",
            2024,
            1000,
            "Flex",
            "Sim",
            "Prata",
            "Ativo"),
        Array.Empty<HistoricoVeiculoEventoDTO>());
}
