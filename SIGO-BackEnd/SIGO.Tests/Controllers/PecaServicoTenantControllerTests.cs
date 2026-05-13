using AutoMapper;
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
    public class PecaServicoTenantControllerTests
    {
        [Fact]
        public async Task PecaPost_DeveForcarOficinaDoJwt_QuandoOficinaCriaPeca()
        {
            var service = new Mock<IPecaService>();
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(s => s.IsInRole(SystemRoles.Admin)).Returns(false);
            currentUser.Setup(s => s.OficinaId).Returns(8);
            service.Setup(s => s.CreateForOficina(It.IsAny<PecaDTO>(), 8)).Returns(Task.CompletedTask);
            var controller = new PecaController(service.Object, Mock.Of<IMapper>(), currentUser.Object);

            var result = await controller.Post(CriarPeca(idOficina: 999));

            Assert.IsType<OkObjectResult>(result);
            service.Verify(s => s.CreateForOficina(It.IsAny<PecaDTO>(), 8), Times.Once);
            service.Verify(s => s.Create(It.IsAny<PecaDTO>()), Times.Never);
        }

        [Fact]
        public async Task PecaGetAll_DeveFiltrarPorOficina_QuandoUsuarioNaoEAdmin()
        {
            var service = new Mock<IPecaService>();
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(s => s.IsInRole(SystemRoles.Admin)).Returns(false);
            currentUser.Setup(s => s.OficinaId).Returns(8);
            service.Setup(s => s.GetByOficina(8)).ReturnsAsync(new[] { CriarPeca(idOficina: 8) });
            var controller = new PecaController(service.Object, Mock.Of<IMapper>(), currentUser.Object);

            var result = await controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Response>(ok.Value);
            var data = Assert.IsAssignableFrom<IEnumerable<PecaDTO>>(response.Data);
            Assert.All(data, p => Assert.Equal(8, p.IdOficina));
            service.Verify(s => s.GetAll(), Times.Never);
        }

        [Fact]
        public async Task ServicoPost_DeveForcarOficinaDoJwt_QuandoOficinaCriaServico()
        {
            var service = new Mock<IServicoService>();
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(s => s.IsInRole(SystemRoles.Admin)).Returns(false);
            currentUser.Setup(s => s.OficinaId).Returns(3);
            service.Setup(s => s.CreateForOficina(It.IsAny<ServicoDTO>(), 3)).Returns(Task.CompletedTask);
            var controller = new ServicoController(service.Object, Mock.Of<IMapper>(), currentUser.Object);

            var result = await controller.Post(CriarServico(idOficina: 999));

            Assert.IsType<OkObjectResult>(result);
            service.Verify(s => s.CreateForOficina(It.IsAny<ServicoDTO>(), 3), Times.Once);
            service.Verify(s => s.Create(It.IsAny<ServicoDTO>()), Times.Never);
        }

        private static PecaDTO CriarPeca(int? idOficina)
        {
            return new PecaDTO
            {
                Nome = "Filtro",
                Tipo = "Oleo",
                Descricao = "Filtro de oleo",
                Valor = 50,
                Quantidade = 2,
                Garantia = DateOnly.FromDateTime(DateTime.Today),
                Unidade = 1,
                IdMarca = 1,
                DataAquisicao = DateOnly.FromDateTime(DateTime.Today),
                Fornecedor = "Fornecedor",
                IdOficina = idOficina
            };
        }

        private static ServicoDTO CriarServico(int? idOficina)
        {
            return new ServicoDTO
            {
                Nome = "Troca de oleo",
                Descricao = "Troca",
                Valor = 100,
                Garantia = DateOnly.FromDateTime(DateTime.Today),
                IdOficina = idOficina
            };
        }
    }
}
