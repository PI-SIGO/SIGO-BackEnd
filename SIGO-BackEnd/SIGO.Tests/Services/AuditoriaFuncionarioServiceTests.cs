using AutoMapper;
using Moq;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Security;
using SIGO.Services.Entities;
using Xunit;

namespace SIGO.Tests.Services;

public sealed class AuditoriaFuncionarioServiceTests
{
    [Fact]
    public async Task Get_OficinaAutenticadaDeveUsarOficinaIdDoJwt()
    {
        var repository = new Mock<IAuditoriaFuncionarioRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var mapper = new Mock<IMapper>();
        var logs = new[]
        {
            new AuditoriaFuncionario { Id = 1, FuncionarioId = 10, FuncionarioNome = "Funcionário A" }
        };
        var expected = new[]
        {
            new AuditoriaFuncionarioDTO { Id = 1, FuncionarioId = 10, FuncionarioNome = "Funcionário A" }
        };
        currentUser.Setup(user => user.IsInRole(SystemRoles.Admin)).Returns(false);
        currentUser.Setup(user => user.IsInRole(SystemRoles.Oficina)).Returns(true);
        currentUser.Setup(user => user.OficinaId).Returns(1);
        repository
            .Setup(item => item.Get(20, null, null, null, null, 1))
            .ReturnsAsync(logs);
        mapper
            .Setup(item => item.Map<IEnumerable<AuditoriaFuncionarioDTO>>(logs))
            .Returns(expected);
        var service = new AuditoriaFuncionarioService(
            repository.Object,
            currentUser.Object,
            mapper.Object);

        var result = await service.Get(funcionarioId: 20);

        Assert.Same(expected, result);
        repository.Verify(item => item.Get(20, null, null, null, null, 1), Times.Once);
        repository.Verify(item => item.Get(20, null, null, null, null, 2), Times.Never);
    }

    [Fact]
    public async Task Get_OficinaSemOficinaIdDeveNegarConsulta()
    {
        var repository = new Mock<IAuditoriaFuncionarioRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(user => user.IsInRole(SystemRoles.Admin)).Returns(false);
        currentUser.Setup(user => user.IsInRole(SystemRoles.Oficina)).Returns(true);
        currentUser.Setup(user => user.OficinaId).Returns((int?)null);
        var service = new AuditoriaFuncionarioService(
            repository.Object,
            currentUser.Object,
            Mock.Of<IMapper>());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.Get());

        repository.Verify(
            item => item.Get(
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int?>()),
            Times.Never);
    }
}
