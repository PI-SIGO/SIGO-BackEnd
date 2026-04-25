namespace SIGO.Services.Interfaces
{
    public interface IReportService
    {
        Task<byte[]> GenerateVehicleHistoryPdfAsync(int veiculoId, DateTime? from = null, DateTime? to = null, string? tipoServico = null);
    }
}
