using SIGO.Objects.Dtos.Entities;

namespace SIGO.Services.Interfaces
{
    public interface IReportService
    {
        Task<HistoricoVeiculoDTO> GetVehicleHistoryAsync(
            int veiculoId,
            DateTime? from = null,
            DateTime? to = null,
            string? tipoServico = null,
            CancellationToken cancellationToken = default);

        Task<byte[]> GenerateVehicleHistoryPdfAsync(
            int veiculoId,
            DateTime? from = null,
            DateTime? to = null,
            string? tipoServico = null,
            CancellationToken cancellationToken = default);

        Task<byte[]> GenerateVehicleHistoryExcelAsync(
            int veiculoId,
            DateTime? from = null,
            DateTime? to = null,
            string? tipoServico = null,
            CancellationToken cancellationToken = default);

        Task<bool> CanAccessVehicleHistoryAsync(
            int veiculoId,
            CancellationToken cancellationToken = default);
    }
}
