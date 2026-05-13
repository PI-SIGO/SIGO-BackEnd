using Microsoft.AspNetCore.Mvc;
using Moq;
using SIGO.Controllers;
using SIGO.Services.Interfaces;
using Xunit;

namespace SIGO.Tests.Controllers
{
    public class ReportControllerSecurityTests
    {
        [Fact]
        public async Task GetVehicleHistoryPdf_DeveRetornarForbid_QuandoUsuarioNaoTemAcessoAoVeiculo()
        {
            var reportService = new Mock<IReportService>();
            reportService.Setup(s => s.CanAccessVehicleHistoryAsync(10)).ReturnsAsync(false);
            var controller = new ReportController(reportService.Object);

            var result = await controller.GetVehicleHistoryPdf(10);

            Assert.IsType<ForbidResult>(result);
            reportService.Verify(s => s.GenerateVehicleHistoryPdfAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task GetVehicleHistoryPdf_DeveGerarPdf_QuandoUsuarioTemAcessoAoVeiculo()
        {
            var reportService = new Mock<IReportService>();
            reportService.Setup(s => s.CanAccessVehicleHistoryAsync(10)).ReturnsAsync(true);
            reportService.Setup(s => s.GenerateVehicleHistoryPdfAsync(10, null, null, null)).ReturnsAsync(new byte[] { 1, 2, 3 });
            var controller = new ReportController(reportService.Object);

            var result = await controller.GetVehicleHistoryPdf(10);

            Assert.IsType<FileContentResult>(result);
        }
    }
}
