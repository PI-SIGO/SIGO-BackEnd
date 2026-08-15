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
        public async Task PecaPost_ForcaOficinaDoJwtERetornaCreated()
        {
            var service = new Mock<IPecaService>();
            var currentUser = CreateOfficeUser(8);
            var request = CreatePiece(idOficina: 999);
            service.Setup(item => item.CreateForOficina(request, 8))
                .Callback(() => request.Id = 41)
                .Returns(Task.CompletedTask);
            var controller = new PecaController(service.Object, currentUser.Object, CreateAuditService());

            var result = await controller.Post(request);

            var created = Assert.IsType<CreatedResult>(result.Result);
            Assert.Equal("/api/v1/pecas/41", created.Location);
            service.Verify(item => item.CreateForOficina(request, 8), Times.Once);
            service.Verify(item => item.Create(It.IsAny<PecaDTO>()), Times.Never);
        }

        [Fact]
        public async Task PecaGetAll_FiltraOficinaEPagina()
        {
            var service = new Mock<IPecaService>();
            var currentUser = CreateOfficeUser(8);
            service.Setup(item => item.GetByOficina(8)).ReturnsAsync(new[]
            {
                CreatePiece(8, id: 1),
                CreatePiece(8, id: 2),
                CreatePiece(8, id: 3)
            });
            var controller = new PecaController(service.Object, currentUser.Object, CreateAuditService());

            var result = await controller.GetAll(new PaginationRequest { Page = 1, PageSize = 2 });

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var page = Assert.IsType<PagedResponse<PecaDTO>>(ok.Value);
            Assert.Equal(3, page.TotalItems);
            Assert.Equal(2, page.Items.Count);
            Assert.All(page.Items, piece => Assert.Equal(8, piece.IdOficina));
        }

        [Fact]
        public async Task ServicoPost_ForcaOficinaDoJwtERetornaCreated()
        {
            var service = new Mock<IServicoService>();
            var currentUser = CreateOfficeUser(3);
            var request = CreateService(idOficina: 999);
            service.Setup(item => item.CreateForOficina(request, 3))
                .Callback(() => request.Id = 52)
                .Returns(Task.CompletedTask);
            var controller = new ServicoController(service.Object, currentUser.Object, CreateAuditService());

            var result = await controller.Post(request);

            var created = Assert.IsType<CreatedResult>(result.Result);
            Assert.Equal("/api/v1/servicos/52", created.Location);
            service.Verify(item => item.CreateForOficina(request, 3), Times.Once);
            service.Verify(item => item.Create(It.IsAny<ServicoDTO>()), Times.Never);
        }

        [Fact]
        public async Task PecaDelete_RetornaNoContent()
        {
            var service = new Mock<IPecaService>();
            var currentUser = CreateAdminUser();
            service.Setup(item => item.GetById(4)).ReturnsAsync(CreatePiece(8, id: 4));
            service.Setup(item => item.Remove(4)).Returns(Task.CompletedTask);
            var controller = new PecaController(service.Object, currentUser.Object, CreateAuditService());

            var result = await controller.Delete(4);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task ServicoDelete_RetornaNoContent()
        {
            var service = new Mock<IServicoService>();
            var currentUser = CreateAdminUser();
            service.Setup(item => item.GetById(4)).ReturnsAsync(CreateService(8));
            service.Setup(item => item.Remove(4)).Returns(Task.CompletedTask);
            var controller = new ServicoController(service.Object, currentUser.Object, CreateAuditService());

            var result = await controller.Delete(4);

            Assert.IsType<NoContentResult>(result);
        }

        private static Mock<ICurrentUserService> CreateOfficeUser(int oficinaId)
        {
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(user => user.IsInRole(SystemRoles.Admin)).Returns(false);
            currentUser.Setup(user => user.IsInRole(SystemRoles.Oficina)).Returns(true);
            currentUser.Setup(user => user.OficinaId).Returns(oficinaId);
            return currentUser;
        }

        private static IAuditoriaFuncionarioService CreateAuditService() =>
            new Mock<IAuditoriaFuncionarioService>().Object;

        private static Mock<ICurrentUserService> CreateAdminUser()
        {
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(user => user.IsInRole(SystemRoles.Admin)).Returns(true);
            return currentUser;
        }

        private static PecaDTO CreatePiece(int? idOficina, int id = 0) => new()
        {
            Id = id,
            Nome = "Filtro",
            EAN = "7891234567890",
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

        private static ServicoDTO CreateService(int? idOficina) => new()
        {
            Nome = "Troca de oleo",
            Descricao = "Troca",
            Valor = 100,
            Garantia = DateOnly.FromDateTime(DateTime.Today),
            IdOficina = idOficina
        };
    }
}
