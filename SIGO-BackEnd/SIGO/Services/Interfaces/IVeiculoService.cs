using Microsoft.AspNetCore.Http;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;

namespace SIGO.Services.Interfaces
{
    public interface IVeiculoService : IGenericService<Veiculo, VeiculoDTO>
    {
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
        Task CreateForCliente(VeiculoDTO veiculoDto, int clienteId);
        Task CreateForOficina(VeiculoDTO veiculoDto, int oficinaId);
        Task<IReadOnlyCollection<VeiculoImagemDTO>> AddImagens(
            int veiculoId,
            IReadOnlyCollection<IFormFile> imagens,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<VeiculoImagemDTO>> AddImagensForCliente(
            int veiculoId,
            int clienteId,
            IReadOnlyCollection<IFormFile> imagens,
            CancellationToken cancellationToken = default);
        Task<VeiculoImagemArquivoDTO> GetImagemArquivo(int veiculoId, string nomeArquivo);
        Task<VeiculoImagemArquivoDTO> GetImagemArquivoForCliente(int veiculoId, int clienteId, string nomeArquivo);
        Task<VeiculoImagemArquivoDTO> GetImagemArquivoForOficina(int veiculoId, int oficinaId, string nomeArquivo);
        Task RemoveImagem(int veiculoId, int imagemId);
        Task RemoveImagemForCliente(int veiculoId, int clienteId, int imagemId);
        Task UpdateVeiculoForCliente(VeiculoDTO veiculoDto, int id, int clienteId);
        Task UpdateVeiculoForOficina(VeiculoDTO veiculoDto, int id, int oficinaId);
        Task UpdateVeiculo(VeiculoDTO veiculoDto, int id);
    }
}
