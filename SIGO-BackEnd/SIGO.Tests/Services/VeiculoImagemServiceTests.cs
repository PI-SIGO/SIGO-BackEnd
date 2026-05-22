using AutoMapper;
using Microsoft.AspNetCore.Http;
using Moq;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Services.Entities;
using SIGO.Services.Interfaces;
using Xunit;

namespace SIGO.Tests.Services
{
    public class VeiculoImagemServiceTests
    {
        private readonly Mock<IVeiculoRepository> _veiculoRepositoryMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<IVeiculoImagemStorageService> _storageServiceMock = new();

        [Fact]
        public async Task AddImagensForCliente_DevePersistirImagemSomenteQuandoVeiculoPertenceAoCliente()
        {
            var imagem = Mock.Of<IFormFile>();
            var veiculo = new Veiculo { Id = 4, ClienteId = 5 };
            var imagemSalva = new VeiculoImagem
            {
                Id = 8,
                VeiculoId = 4,
                Url = "/api/veiculos/4/imagens/foto.png",
                NomeArquivo = "foto.png",
                NomeOriginal = "foto.png",
                ContentType = "image/png",
                TamanhoBytes = 12,
                CriadoEm = DateTime.UtcNow
            };
            _veiculoRepositoryMock.Setup(r => r.GetByIdForCliente(4, 5)).ReturnsAsync(veiculo);
            _veiculoRepositoryMock.Setup(r => r.SaveChanges()).ReturnsAsync(1);
            _storageServiceMock
                .Setup(s => s.SaveAsync(4, imagem, It.IsAny<CancellationToken>()))
                .ReturnsAsync(imagemSalva);
            _mapperMock
                .Setup(m => m.Map<IReadOnlyCollection<VeiculoImagemDTO>>(It.IsAny<object>()))
                .Returns(new List<VeiculoImagemDTO>
                {
                    new()
                    {
                        Id = 8,
                        VeiculoId = 4,
                        Url = "/api/veiculos/4/imagens/foto.png",
                        NomeOriginal = "foto.png",
                        ContentType = "image/png",
                        TamanhoBytes = 12,
                        CriadoEm = imagemSalva.CriadoEm
                    }
                });
            var service = CreateService();

            var result = await service.AddImagensForCliente(
                4,
                5,
                new List<IFormFile> { imagem },
                CancellationToken.None);

            Assert.Single(result);
            Assert.Contains(imagemSalva, veiculo.Imagens);
            _veiculoRepositoryMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task AddImagensForCliente_DeveFalharSemSalvarArquivo_QuandoVeiculoNaoPertenceAoCliente()
        {
            _veiculoRepositoryMock.Setup(r => r.GetByIdForCliente(4, 5)).ReturnsAsync((Veiculo)null);
            var service = CreateService();

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.AddImagensForCliente(
                4,
                5,
                new List<IFormFile> { Mock.Of<IFormFile>() },
                CancellationToken.None));

            _storageServiceMock.Verify(s => s.SaveAsync(
                It.IsAny<int>(),
                It.IsAny<IFormFile>(),
                It.IsAny<CancellationToken>()), Times.Never);
            _veiculoRepositoryMock.Verify(r => r.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task RemoveImagemForCliente_DeveRemoverImagemDoVeiculoEArquivo()
        {
            var imagem = new VeiculoImagem { Id = 8, VeiculoId = 4, Url = "/api/veiculos/4/imagens/foto.png" };
            var veiculo = new Veiculo
            {
                Id = 4,
                ClienteId = 5,
                Imagens = new List<VeiculoImagem> { imagem }
            };
            _veiculoRepositoryMock.Setup(r => r.GetByIdForCliente(4, 5)).ReturnsAsync(veiculo);
            _veiculoRepositoryMock.Setup(r => r.SaveChanges()).ReturnsAsync(1);
            var service = CreateService();

            await service.RemoveImagemForCliente(4, 5, 8);

            Assert.DoesNotContain(imagem, veiculo.Imagens);
            _veiculoRepositoryMock.Verify(r => r.SaveChanges(), Times.Once);
            _storageServiceMock.Verify(s => s.Delete(imagem), Times.Once);
        }

        [Fact]
        public async Task GetImagemArquivoForCliente_DeveAbrirSomenteImagemDoVeiculoDoCliente()
        {
            var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var imagem = new VeiculoImagem
            {
                Id = 8,
                VeiculoId = 4,
                NomeArquivo = "foto.png",
                NomeOriginal = "foto.png",
                ContentType = "image/png"
            };
            var veiculo = new Veiculo
            {
                Id = 4,
                ClienteId = 5,
                Imagens = new List<VeiculoImagem> { imagem }
            };
            _veiculoRepositoryMock.Setup(r => r.GetByIdForCliente(4, 5)).ReturnsAsync(veiculo);
            _storageServiceMock.Setup(s => s.OpenRead(imagem)).Returns(stream);
            var service = CreateService();

            var result = await service.GetImagemArquivoForCliente(4, 5, "foto.png");

            Assert.Same(stream, result.Conteudo);
            Assert.Equal("image/png", result.ContentType);
            Assert.Equal("foto.png", result.NomeOriginal);
        }

        private VeiculoService CreateService()
        {
            return new VeiculoService(
                _veiculoRepositoryMock.Object,
                _mapperMock.Object,
                Mock.Of<IClienteRepository>(),
                _storageServiceMock.Object);
        }
    }
}
