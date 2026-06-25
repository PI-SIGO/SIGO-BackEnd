using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SIGO.Controllers;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;
using Xunit;

namespace SIGO.Tests.Controllers
{
    public class VeiculoControllerTests
    {
        private readonly Mock<IVeiculoService> _veiculoServiceMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

        [Fact]
        public async Task Get_DeveFiltrarVeiculosDoClienteLogado()
        {
            _veiculoServiceMock.Setup(s => s.GetByCliente(5)).ReturnsAsync(new List<VeiculoDTO>
            {
                CriarVeiculoDto(id: 1, clienteId: 5),
                CriarVeiculoDto(id: 3, clienteId: 5)
            });

            var controller = CreateController(userId: 5, roles: new[] { SystemRoles.Cliente });

            var result = await controller.Get();

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Response>(ok.Value);
            var data = Assert.IsAssignableFrom<IEnumerable<VeiculoDTO>>(response.Data);
            Assert.All(data, veiculo => Assert.Equal(5, veiculo.ClienteId));
            Assert.Equal(2, data.Count());
            _veiculoServiceMock.Verify(s => s.GetAll(), Times.Never);
        }

        [Fact]
        public async Task Get_DeveRetornarVeiculoComDadosAtrelados()
        {
            var veiculo = CriarVeiculoDto(id: 1, clienteId: 5);
            veiculo.Imagens.Add(new VeiculoImagemDTO { Id = 10, VeiculoId = 1, NomeOriginal = "frente.png" });
            veiculo.Marcas.Add(new MarcaDTO { Id = 20, Nome = "Fiat", Desc = "Marca", TipoMarca = "Automovel" });
            veiculo.RegistroServicos.Add(new RegistroServicoDTO
            {
                Id = 30,
                VeiculoId = 1,
                Descricao = "Revisao",
                Responsavel = "Mecanico",
                PecasSubstituidas = new List<PecaSubstituidaDTO>
                {
                    new() { Id = 40, RegistroServicoId = 30, Nome = "Filtro", Quantidade = 1 }
                }
            });
            veiculo.Pedidos.Add(new PedidoDTO
            {
                Id = 50,
                idCliente = 5,
                idVeiculo = 1,
                Pedido_Pecas = new List<Pedido_PecaDTO>
                {
                    new() { IdPedido = 50, IdPeca = 60, Quantidade = 1 }
                },
                Pedido_Servicos = new List<Pedido_ServicoDTO>
                {
                    new() { IdPedido = 50, IdServico = 70, QuantVezes = 1 }
                }
            });

            _veiculoServiceMock.Setup(s => s.GetByCliente(5)).ReturnsAsync(new List<VeiculoDTO> { veiculo });

            var controller = CreateController(userId: 5, roles: new[] { SystemRoles.Cliente });

            var result = await controller.Get();

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Response>(ok.Value);
            var data = Assert.IsAssignableFrom<IEnumerable<VeiculoDTO>>(response.Data);
            var item = Assert.Single(data);

            Assert.Single(item.Imagens);
            Assert.Single(item.Marcas);
            Assert.Single(item.RegistroServicos);
            Assert.Single(item.RegistroServicos.Single().PecasSubstituidas);
            Assert.Single(item.Pedidos);
            Assert.Single(item.Pedidos.Single().Pedido_Pecas);
            Assert.Single(item.Pedidos.Single().Pedido_Servicos);
        }

        [Fact]
        public async Task Delete_DeveRetornarForbid_QuandoFuncionarioTentaExcluirVeiculoGlobal()
        {
            var controller = CreateController(userId: 10, oficinaId: 2, roles: new[] { SystemRoles.Funcionario });

            var result = await controller.Delete(4);

            Assert.IsType<ForbidResult>(result);
            _veiculoServiceMock.Verify(s => s.Remove(It.IsAny<int>()), Times.Never);
            _veiculoServiceMock.Verify(s => s.GetByIdForOficina(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Create_DeveCadastrarVeiculoParaClienteVinculadoDaOficina()
        {
            var veiculo = CriarVeiculoDto(id: 0, clienteId: 5);
            _veiculoServiceMock
                .Setup(s => s.CreateForOficina(veiculo, 2))
                .Returns(Task.CompletedTask);

            var controller = CreateController(oficinaId: 2, roles: new[] { SystemRoles.Oficina });

            var result = await controller.Create(veiculo);

            Assert.IsType<OkObjectResult>(result);
            _veiculoServiceMock.Verify(s => s.CreateForOficina(veiculo, 2), Times.Once);
            _veiculoServiceMock.Verify(s => s.CreateForCliente(It.IsAny<VeiculoDTO>(), It.IsAny<int>()), Times.Never);
            _veiculoServiceMock.Verify(s => s.Create(It.IsAny<VeiculoDTO>()), Times.Never);
        }

        [Fact]
        public async Task Create_DeveRetornarForbid_QuandoOficinaNaoTemOficinaId()
        {
            var controller = CreateController(roles: new[] { SystemRoles.Oficina });

            var result = await controller.Create(CriarVeiculoDto(id: 0, clienteId: 5));

            Assert.IsType<ForbidResult>(result);
            _veiculoServiceMock.Verify(s => s.CreateForOficina(It.IsAny<VeiculoDTO>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task AddImagens_DeveCadastrarImagemNoVeiculoDoClienteLogado()
        {
            var imagens = new List<IFormFile> { CriarImagem() };
            var imagensSalvas = new List<VeiculoImagemDTO>
            {
                new()
                {
                    Id = 8,
                    VeiculoId = 4,
                    Url = "/api/veiculos/4/imagens/foto.png",
                    NomeOriginal = "foto.png",
                    ContentType = "image/png",
                    TamanhoBytes = 12,
                    CriadoEm = DateTime.UtcNow
                }
            };
            _veiculoServiceMock
                .Setup(s => s.AddImagensForCliente(
                    4,
                    5,
                    It.Is<IReadOnlyCollection<IFormFile>>(files => files.Count == 1),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(imagensSalvas);

            var controller = CreateController(userId: 5, roles: new[] { SystemRoles.Cliente });

            var result = await controller.AddImagens(4, imagens, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Response>(ok.Value);
            var data = Assert.IsAssignableFrom<IEnumerable<VeiculoImagemDTO>>(response.Data);
            Assert.Single(data);
            Assert.Equal(ResponseEnum.SUCCESS, response.Code);
            _veiculoServiceMock.Verify(s => s.AddImagens(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<IFormFile>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AddImagens_DeveCadastrarImagemNoVeiculoDaOficina()
        {
            var imagens = new List<IFormFile> { CriarImagem() };
            var imagensSalvas = new List<VeiculoImagemDTO>
            {
                new()
                {
                    Id = 8,
                    VeiculoId = 4,
                    Url = "/api/veiculos/4/imagens/foto.png",
                    NomeOriginal = "foto.png",
                    ContentType = "image/png",
                    TamanhoBytes = 12,
                    CriadoEm = DateTime.UtcNow
                }
            };
            _veiculoServiceMock
                .Setup(s => s.AddImagensForOficina(
                    4,
                    2,
                    It.Is<IReadOnlyCollection<IFormFile>>(files => files.Count == 1),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(imagensSalvas);

            var controller = CreateController(oficinaId: 2, roles: new[] { SystemRoles.Oficina });

            var result = await controller.AddImagens(4, imagens, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Response>(ok.Value);
            var data = Assert.IsAssignableFrom<IEnumerable<VeiculoImagemDTO>>(response.Data);
            Assert.Single(data);
            _veiculoServiceMock.Verify(s => s.AddImagensForOficina(
                4,
                2,
                It.IsAny<IReadOnlyCollection<IFormFile>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AddImagens_DeveRetornarForbid_QuandoClienteNaoTemUserId()
        {
            var controller = CreateController(roles: new[] { SystemRoles.Cliente });

            var result = await controller.AddImagens(4, new List<IFormFile> { CriarImagem() }, CancellationToken.None);

            Assert.IsType<ForbidResult>(result);
            _veiculoServiceMock.Verify(s => s.AddImagensForCliente(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<IReadOnlyCollection<IFormFile>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Update_DeveAtualizarVeiculoDaOficina()
        {
            var veiculo = CriarVeiculoDto(id: 4, clienteId: 5);
            _veiculoServiceMock
                .Setup(s => s.UpdateVeiculoForOficina(veiculo, 4, 2))
                .Returns(Task.CompletedTask);

            var controller = CreateController(oficinaId: 2, roles: new[] { SystemRoles.Oficina });

            var result = await controller.Update(4, veiculo);

            Assert.IsType<OkObjectResult>(result);
            _veiculoServiceMock.Verify(s => s.UpdateVeiculoForOficina(veiculo, 4, 2), Times.Once);
            _veiculoServiceMock.Verify(s => s.UpdateVeiculoForCliente(It.IsAny<VeiculoDTO>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            _veiculoServiceMock.Verify(s => s.UpdateVeiculo(It.IsAny<VeiculoDTO>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteImagem_DeveRemoverImagemDoVeiculoDoClienteLogado()
        {
            _veiculoServiceMock
                .Setup(s => s.RemoveImagemForCliente(4, 5, 8))
                .Returns(Task.CompletedTask);

            var controller = CreateController(userId: 5, roles: new[] { SystemRoles.Cliente });

            var result = await controller.DeleteImagem(4, 8);

            Assert.IsType<OkObjectResult>(result);
            _veiculoServiceMock.Verify(s => s.RemoveImagemForCliente(4, 5, 8), Times.Once);
            _veiculoServiceMock.Verify(s => s.RemoveImagem(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetImagemArquivo_DeveRetornarArquivoDoVeiculoDoClienteLogado()
        {
            var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            _veiculoServiceMock
                .Setup(s => s.GetImagemArquivoForCliente(4, 5, "foto.png"))
                .ReturnsAsync(new VeiculoImagemArquivoDTO
                {
                    Conteudo = stream,
                    ContentType = "image/png",
                    NomeOriginal = "foto.png"
                });

            var controller = CreateController(userId: 5, roles: new[] { SystemRoles.Cliente });

            var result = await controller.GetImagemArquivo(4, "foto.png");

            var file = Assert.IsType<FileStreamResult>(result);
            Assert.Equal("image/png", file.ContentType);
            Assert.True(file.EnableRangeProcessing);
            _veiculoServiceMock.Verify(s => s.GetImagemArquivo(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        private VeiculoController CreateController(int? userId = null, int? oficinaId = null, params string[] roles)
        {
            var controller = new VeiculoController(
                _veiculoServiceMock.Object,
                _mapperMock.Object,
                _currentUserServiceMock.Object);

            _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
            _currentUserServiceMock.Setup(s => s.OficinaId).Returns(oficinaId);
            _currentUserServiceMock.Setup(s => s.IsInRole(It.IsAny<string>()))
                .Returns<string>(role => roles.Contains(role));
            return controller;
        }

        private static VeiculoDTO CriarVeiculoDto(int id, int clienteId)
        {
            return new VeiculoDTO
            {
                Id = id,
                NomeVeiculo = "Carro",
                TipoVeiculo = "Hatch",
                PlacaVeiculo = "ABC1234",
                ChassiVeiculo = $"CHASSI{id}",
                AnoFab = 2020,
                Quilometragem = 10000,
                Combustivel = "Gasolina",
                Seguro = "Ativo",
                Cor = "Preto",
                ClienteId = clienteId
            };
        }

        private static IFormFile CriarImagem()
        {
            var bytes = new byte[]
            {
                0x89, 0x50, 0x4E, 0x47,
                0x0D, 0x0A, 0x1A, 0x0A,
                0x00, 0x00, 0x00, 0x00
            };
            var stream = new MemoryStream(bytes);

            return new FormFile(stream, 0, bytes.Length, "imagens", "foto.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
        }
    }
}
