using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGO.Security;
using SIGO.Services.Interfaces;

namespace SIGO.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = AuthorizationPolicies.SelfServiceAccess)]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("vehicle/{veiculoId}")]
        public async Task<IActionResult> GetVehicleHistoryPdf(int veiculoId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string? tipo = null)
        {
            if (!await _reportService.CanAccessVehicleHistoryAsync(veiculoId))
                return Forbid();

            var pdf = await _reportService.GenerateVehicleHistoryPdfAsync(veiculoId, from, to, tipo);
            var fileName = $"relatorio_veiculo_{veiculoId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            return File(pdf, "application/pdf", fileName);
        }
    }
}
