using Microsoft.AspNetCore.Authorization;
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
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly Mock<IAuditoriaFuncionarioService> _auditoriaServiceMock = new();

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
            var response = Assert.IsType<PagedResponse<VeiculoDTO>>(ok.Value);
            Assert.All(response.Items, veiculo => Assert.Equal(5, veiculo.ClienteId));
            Assert.Equal(2, response.TotalItems);
            _veiculoServiceMock.Verify(s => s.GetAll(), Times.Never);
        }

        [Fact]
        public async Task Get_DeveRetornarVeiculoComDadosAtrelados()
        {
            var veiculo = CriarVeiculoDto(id: 1, clienteId: 5);
            veiculo.Imagens.Add(new VeiculoImagemDTO { Id = 10, VeiculoId = 1, NomeOriginal = "frente.png" });
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
            var response = Assert.IsType<PagedResponse<VeiculoDTO>>(ok.Value);
            var item = Assert.Single(response.Items);

            Assert.Single(item.Imagens);
            Assert.Single(item.RegistroServicos);
            Assert.Single(item.RegistroServicos.Single().PecasSubstituidas);
            Assert.Single(item.Pedidos);
            Assert.Single(item.Pedidos.Single().Pedido_Pecas);
            Assert.Single(item.Pedidos.Single().Pedido_Servicos);
        }

        [Fact]
        public async Task Get_DeveAplicarPaginacaoDepoisDoEscopoDoCliente()
        {
            _veiculoServiceMock.Setup(s => s.GetByCliente(5)).ReturnsAsync(new[]
            {
                CriarVeiculoDto(id: 1, clienteId: 5),
                CriarVeiculoDto(id: 2, clienteId: 5),
                CriarVeiculoDto(id: 3, clienteId: 5)
            });
            var controller = CreateController(userId: 5, roles: new[] { SystemRoles.Cliente });

            var result = await controller.Get(new PaginationRequest { Page = 2, PageSize = 1 });

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<PagedResponse<VeiculoDTO>>(ok.Value);
            Assert.Equal(3, response.TotalItems);
            Assert.Equal(3, response.TotalPages);
            Assert.Equal(2, Assert.Single(response.Items).Id);
        }

        [Fact]
        public async Task GetById_DeveBuscarSomenteVeiculoDoClienteLogado()
        {
            var veiculo = CriarVeiculoDto(id: 4, clienteId: 5);
            _veiculoServiceMock.Setup(s => s.GetByIdForCliente(4, 5)).ReturnsAsync(veiculo);
            var controller = CreateController(userId: 5, roles: new[] { SystemRoles.Cliente });

            var result = await controller.GetById(4);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Same(veiculo, ok.Value);
            _veiculoServiceMock.Verify(s => s.GetById(4), Times.Never);
        }

        [Fact]
        public async Task GetByPlaca_DeveRetornarPaginaVazia_QuandoNaoHaResultado()
        {
            _veiculoServiceMock.Setup(s => s.GetByPlaca("XYZ")).ReturnsAsync(Array.Empty<VeiculoDTO>());
            var controller = CreateController(roles: new[] { SystemRoles.Admin });

            var result = await controller.GetByPlaca("XYZ");

            var ok = Assert.IsType<OkObjectResult>(result);
            var page = Assert.IsType<PagedResponse<VeiculoDTO>>(ok.Value);
            Assert.Empty(page.Items);
            Assert.Equal(0, page.TotalItems);
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
        public async Task CreateForCliente_DeveCadastrarParaClienteVinculadoDaOficina()
        {
            var request = CriarVeiculoRequest();
            var veiculo = CriarVeiculoDto(id: 9, clienteId: 5);
            _veiculoServiceMock
                .Setup(s => s.CreateForOficina(request, 5, 2))
                .ReturnsAsync(veiculo);

            var controller = CreateController(oficinaId: 2, roles: new[] { SystemRoles.Oficina });

            var result = await controller.CreateForCliente(5, request);

            Assert.IsType<CreatedResult>(result);
            _veiculoServiceMock.Verify(s => s.CreateForOficina(request, 5, 2), Times.Once);
        }

        [Fact]
        public async Task CreateForCliente_DeveCadastrarNoEscopoDaOficinaDoFuncionario()
        {
            var request = CriarVeiculoRequest();
            var veiculo = CriarVeiculoDto(id: 9, clienteId: 5);
            _veiculoServiceMock
                .Setup(s => s.CreateForOficina(request, 5, 2))
                .ReturnsAsync(veiculo);

            var controller = CreateController(oficinaId: 2, roles: new[] { SystemRoles.Funcionario });

            var result = await controller.CreateForCliente(5, request);

            Assert.IsType<CreatedResult>(result);
            _veiculoServiceMock.Verify(s => s.CreateForOficina(request, 5, 2), Times.Once);
        }

        [Fact]
        public async Task CreateForCliente_DeveRetornarForbid_QuandoOficinaNaoTemOficinaId()
        {
            var controller = CreateController(roles: new[] { SystemRoles.Oficina });

            var result = await controller.CreateForCliente(5, CriarVeiculoRequest());

            Assert.IsType<ForbidResult>(result);
            _veiculoServiceMock.Verify(
                s => s.CreateForOficina(
                    It.IsAny<VeiculoRequestDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()),
                Times.Never);
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
                    Url = "/api/v1/veiculos/4/imagens/foto.png",
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

            var created = Assert.IsType<CreatedResult>(result);
            var data = Assert.IsAssignableFrom<IEnumerable<VeiculoImagemDTO>>(created.Value);
            Assert.Single(data);
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
                    Url = "/api/v1/veiculos/4/imagens/foto.png",
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

            var created = Assert.IsType<CreatedResult>(result);
            var data = Assert.IsAssignableFrom<IEnumerable<VeiculoImagemDTO>>(created.Value);
            Assert.Single(data);
            _veiculoServiceMock.Verify(s => s.AddImagensForOficina(
                4,
                2,
                It.IsAny<IReadOnlyCollection<IFormFile>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AddImagens_DeveUsarEscopoDaOficinaDoFuncionario()
        {
            var imagens = new List<IFormFile> { CriarImagem() };
            _veiculoServiceMock
                .Setup(s => s.AddImagensForOficina(
                    4,
                    2,
                    It.IsAny<IReadOnlyCollection<IFormFile>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<VeiculoImagemDTO>());

            var controller = CreateController(oficinaId: 2, roles: new[] { SystemRoles.Funcionario });

            var result = await controller.AddImagens(4, imagens, CancellationToken.None);

            Assert.IsType<CreatedResult>(result);
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
        public async Task Update_DeveRetornarForbid_QuandoOficinaUsaRotaGlobal()
        {
            var request = CriarVeiculoRequest();
            var controller = CreateController(oficinaId: 2, roles: new[] { SystemRoles.Oficina });

            var result = await controller.Update(4, request);

            Assert.IsType<ForbidResult>(result);
            _veiculoServiceMock.Verify(s => s.UpdateVeiculoForOficina(
                It.IsAny<VeiculoRequestDTO>(),
                It.IsAny<int>(),
                It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Update_DeveRetornarForbid_QuandoFuncionarioUsaRotaGlobal()
        {
            var request = CriarVeiculoRequest();
            var controller = CreateController(oficinaId: 2, roles: new[] { SystemRoles.Funcionario });

            var result = await controller.Update(4, request);

            Assert.IsType<ForbidResult>(result);
            _veiculoServiceMock.Verify(s => s.UpdateVeiculoForOficina(
                It.IsAny<VeiculoRequestDTO>(),
                It.IsAny<int>(),
                It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task UpdateForOficina_DeveAtualizarVeiculoNoEscopoDaOficinaDoJwt()
        {
            var request = CriarVeiculoRequest();
            var veiculo = CriarVeiculoDto(id: 4, clienteId: 5);
            _veiculoServiceMock
                .Setup(service => service.UpdateVeiculoForOficina(request, 4, 2))
                .ReturnsAsync(veiculo);
            var controller = CreateController(
                userId: 10,
                oficinaId: 2,
                roles: new[] { SystemRoles.Oficina });

            var result = await controller.UpdateForOficina(4, request);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Same(veiculo, ok.Value);
            _veiculoServiceMock.Verify(service => service.UpdateVeiculoForOficina(
                request,
                4,
                2), Times.Once);
        }

        [Fact]
        public async Task UpdateForOficina_DeveRetornarForbid_QuandoJwtNaoTemOficinaId()
        {
            var controller = CreateController(roles: new[] { SystemRoles.Oficina });

            var result = await controller.UpdateForOficina(4, CriarVeiculoRequest());

            Assert.IsType<ForbidResult>(result);
            _veiculoServiceMock.Verify(service => service.UpdateVeiculoForOficina(
                It.IsAny<VeiculoRequestDTO>(),
                It.IsAny<int>(),
                It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void VeiculoController_NaoDeveExporAtualizacaoDeStatus()
        {
            Assert.Null(typeof(VeiculoController).GetMethod("UpdateStatus"));
        }

        [Fact]
        public async Task DeleteForOficina_DeveExcluirVeiculoNoEscopoDaOficinaDoJwt()
        {
            var controller = CreateController(
                userId: 10,
                oficinaId: 2,
                roles: new[] { SystemRoles.Oficina });

            var result = await controller.DeleteForOficina(4);

            Assert.IsType<NoContentResult>(result);
            _veiculoServiceMock.Verify(service => service.RemoveForOficina(4, 2), Times.Once);
            _veiculoServiceMock.Verify(service => service.Remove(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteForOficina_DeveRetornarForbid_QuandoJwtNaoTemOficinaId()
        {
            var controller = CreateController(roles: new[] { SystemRoles.Oficina });

            var result = await controller.DeleteForOficina(4);

            Assert.IsType<ForbidResult>(result);
            _veiculoServiceMock.Verify(service => service.RemoveForOficina(
                It.IsAny<int>(),
                It.IsAny<int>()), Times.Never);
        }

        [Theory]
        [InlineData(nameof(VeiculoController.CreateForCliente))]
        [InlineData(nameof(VeiculoController.AddImagens))]
        [InlineData(nameof(VeiculoController.UpdateForOficina))]
        [InlineData(nameof(VeiculoController.DeleteForOficina))]
        public void EscritasOperacionais_DevemAutorizarFuncionario(string methodName)
        {
            var attribute = typeof(VeiculoController)
                .GetMethod(methodName)!
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
                .Cast<AuthorizeAttribute>()
                .Single();

            Assert.Contains(SystemRoles.Funcionario, attribute.Roles ?? string.Empty);
        }

        [Fact]
        public async Task DeleteImagem_DeveRemoverImagemDoVeiculoDoClienteLogado()
        {
            _veiculoServiceMock
                .Setup(s => s.RemoveImagemForCliente(4, 5, 8))
                .Returns(Task.CompletedTask);

            var controller = CreateController(userId: 5, roles: new[] { SystemRoles.Cliente });

            var result = await controller.DeleteImagem(4, 8);

            Assert.IsType<NoContentResult>(result);
            _veiculoServiceMock.Verify(s => s.RemoveImagemForCliente(4, 5, 8), Times.Once);
            _veiculoServiceMock.Verify(s => s.RemoveImagem(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void DeleteImagem_DeveAutorizarOficina()
        {
            var attribute = typeof(VeiculoController)
                .GetMethod(nameof(VeiculoController.DeleteImagem))!
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
                .Cast<AuthorizeAttribute>()
                .Single();

            Assert.Contains(SystemRoles.Oficina, attribute.Roles ?? string.Empty);
        }

        [Fact]
        public async Task DeleteImagem_DeveRemoverImagemNoEscopoDaOficina()
        {
            _veiculoServiceMock
                .Setup(service => service.RemoveImagemForOficina(4, 2, 8))
                .Returns(Task.CompletedTask);
            var controller = CreateController(
                oficinaId: 2,
                roles: new[] { SystemRoles.Oficina });

            var result = await controller.DeleteImagem(4, 8);

            Assert.IsType<NoContentResult>(result);
            _veiculoServiceMock.Verify(
                service => service.RemoveImagemForOficina(4, 2, 8),
                Times.Once);
            _veiculoServiceMock.Verify(
                service => service.RemoveImagem(It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
            _veiculoServiceMock.Verify(
                service => service.RemoveImagemForCliente(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteImagem_DeveRetornarForbid_QuandoOficinaNaoTemOficinaId()
        {
            var controller = CreateController(roles: new[] { SystemRoles.Oficina });

            var result = await controller.DeleteImagem(4, 8);

            Assert.IsType<ForbidResult>(result);
            _veiculoServiceMock.Verify(
                service => service.RemoveImagemForOficina(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()),
                Times.Never);
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
                _currentUserServiceMock.Object,
                _auditoriaServiceMock.Object);

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
                ModeloVeiculo = "Hatch",
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

        private static VeiculoRequestDTO CriarVeiculoRequest()
        {
            return new VeiculoRequestDTO
            {
                NomeVeiculo = "Carro",
                ModeloVeiculo = "Hatch",
                PlacaVeiculo = "ABC1234",
                ChassiVeiculo = "9BGKS48U0KG000001",
                AnoFab = 2020,
                Quilometragem = 10000,
                Combustivel = "Gasolina",
                Seguro = "Ativo",
                Cor = "Preto"
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
