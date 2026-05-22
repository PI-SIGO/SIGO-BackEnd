using Microsoft.AspNetCore.Http;
using SIGO.Objects.Models;

namespace SIGO.Services.Interfaces
{
    public interface IVeiculoImagemStorageService
    {
        Task<VeiculoImagem> SaveAsync(
            int veiculoId,
            IFormFile imagem,
            CancellationToken cancellationToken = default);

        Stream OpenRead(VeiculoImagem imagem);

        void Delete(VeiculoImagem imagem);
    }
}
