using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGO.Security;
using SIGO.Services.Interfaces;

namespace SIGO.Controllers
{
    [ApiController]
    [Route("api/v1/relatorios")]
    [Authorize(Policy = AuthorizationPolicies.SelfServiceAccess)]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("veiculos/{veiculoId:int}/historico")]
        public async Task<IActionResult> GetVehicleHistory(
            int veiculoId,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? tipo = null,
            CancellationToken cancellationToken = default)
        {
            if (!await _reportService.CanAccessVehicleHistoryAsync(veiculoId, cancellationToken))
                return Forbid();

            var history = await _reportService.GetVehicleHistoryAsync(
                veiculoId,
                from,
                to,
                tipo,
                cancellationToken);
            return Ok(history);
        }

        [HttpGet("veiculos/{veiculoId:int}")]
        public async Task<IActionResult> GetVehicleHistoryPdf(
            int veiculoId,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? tipo = null,
            CancellationToken cancellationToken = default)
        {
            if (!await _reportService.CanAccessVehicleHistoryAsync(veiculoId, cancellationToken))
                return Forbid();

            var pdf = await _reportService.GenerateVehicleHistoryPdfAsync(
                veiculoId,
                from,
                to,
                tipo,
                cancellationToken);
            var fileName = $"relatorio_veiculo_{veiculoId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            return File(pdf, "application/pdf", fileName);
        }

        [HttpGet("veiculos/{veiculoId:int}/excel")]
        public async Task<IActionResult> GetVehicleHistoryExcel(
            int veiculoId,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? tipo = null,
            CancellationToken cancellationToken = default)
        {
            if (!await _reportService.CanAccessVehicleHistoryAsync(veiculoId, cancellationToken))
                return Forbid();

            var excel = await _reportService.GenerateVehicleHistoryExcelAsync(
                veiculoId,
                from,
                to,
                tipo,
                cancellationToken);
            var fileName = $"historico_veiculo_{veiculoId}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(
                excel,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
