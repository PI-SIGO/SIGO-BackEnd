using Microsoft.AspNetCore.Http;
using SIGO.Objects.Dtos.Entities;

namespace SIGO.Services.Interfaces
{
    public interface IVeiculoService
    {
        Task<IEnumerable<VeiculoDTO>> GetAll();
        Task<VeiculoDTO?> GetById(int id);
        Task Remove(int id);
        Task<IEnumerable<VeiculoDTO>> GetByPlaca(string placa);
        Task<IEnumerable<VeiculoDTO>> GetByPlacaForCliente(string placa, int clienteId);
        Task<IEnumerable<VeiculoDTO>> GetByPlacaForOficina(string placa, int oficinaId);
        Task<IEnumerable<VeiculoDTO>> GetByTipo(string tipo);
        Task<IEnumerable<VeiculoDTO>> GetByTipoForCliente(string tipo, int clienteId);
        Task<IEnumerable<VeiculoDTO>> GetByTipoForOficina(string tipo, int oficinaId);
        Task<IEnumerable<VeiculoDTO>> GetByCliente(int clienteId);
        Task<IEnumerable<VeiculoDTO>> GetByOficina(int oficinaId);
        Task<VeiculoDTO?> GetByIdForCliente(int id, int clienteId);
        Task<VeiculoDTO?> GetByIdForOficina(int id, int oficinaId);
        Task<VeiculoDTO> CreateVeiculo(VeiculoRequestDTO request, int clienteId);
        Task<VeiculoDTO> CreateForOficina(VeiculoRequestDTO request, int clienteId, int oficinaId);
        Task<IReadOnlyCollection<VeiculoImagemDTO>> AddImagens(
            int veiculoId,
            IReadOnlyCollection<IFormFile> imagens,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<VeiculoImagemDTO>> AddImagensForCliente(
            int veiculoId,
            int clienteId,
            IReadOnlyCollection<IFormFile> imagens,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<VeiculoImagemDTO>> AddImagensForOficina(
            int veiculoId,
            int oficinaId,
            IReadOnlyCollection<IFormFile> imagens,
            CancellationToken cancellationToken = default);
        Task<VeiculoImagemArquivoDTO> GetImagemArquivo(int veiculoId, string nomeArquivo);
        Task<VeiculoImagemArquivoDTO> GetImagemArquivoForCliente(int veiculoId, int clienteId, string nomeArquivo);
        Task<VeiculoImagemArquivoDTO> GetImagemArquivoForOficina(int veiculoId, int oficinaId, string nomeArquivo);
        Task RemoveImagem(int veiculoId, int imagemId);
        Task RemoveImagemForCliente(int veiculoId, int clienteId, int imagemId);
        Task RemoveImagemForOficina(int veiculoId, int oficinaId, int imagemId);
        Task RemoveForOficina(int veiculoId, int oficinaId);
        Task<VeiculoDTO> UpdateVeiculoForCliente(VeiculoRequestDTO request, int id, int clienteId);
        Task<VeiculoDTO> UpdateVeiculoForOficina(VeiculoRequestDTO request, int id, int oficinaId);
        Task<VeiculoDTO> UpdateVeiculo(VeiculoRequestDTO request, int id);
    }
}
