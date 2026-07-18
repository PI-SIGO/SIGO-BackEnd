using AutoMapper;
using Moq;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Services.Entities;
using SIGO.Validation;
using Xunit;

namespace SIGO.Tests.Services
{
    public class ServicoServiceTests
    {
        private readonly Mock<IServicoRepository> _services = new();
        private readonly Mock<IFuncionarioRepository> _employees = new();
        private readonly Mock<IMapper> _mapper = new();

        [Fact]
        public async Task Update_SincronizaFuncionariosDaMesmaOficina()
        {
            var existing = new Servico { Id = 4, IdOficina = 7 };
            var request = CreateRequest(7, employeeId: 12);
            _services.Setup(repository => repository.GetById(4)).ReturnsAsync(existing);
            _employees.Setup(repository => repository.ExistsInOficina(12, 7)).ReturnsAsync(true);
            _services.Setup(repository => repository.SaveWithEmployeesAsync(
                    existing,
                    It.IsAny<IReadOnlyCollection<Funcionario_Servico>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await CreateService().Update(request, 4);

            Assert.Equal(4, request.Id);
            Assert.Equal(4, Assert.Single(request.Funcionario_Servicos).IdServico);
            _services.Verify(repository => repository.SaveWithEmployeesAsync(
                existing,
                It.Is<IReadOnlyCollection<Funcionario_Servico>>(items =>
                    items.Count == 1 && items.Single().IdFuncionario == 12),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Update_RejeitaFuncionarioDeOutraOficina()
        {
            var existing = new Servico { Id = 4, IdOficina = 7 };
            var request = CreateRequest(7, employeeId: 99);
            _services.Setup(repository => repository.GetById(4)).ReturnsAsync(existing);
            _employees.Setup(repository => repository.ExistsInOficina(99, 7)).ReturnsAsync(false);

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(
                () => CreateService().Update(request, 4));

            Assert.Contains(exception.Errors, error =>
                error.Field == nameof(ServicoDTO.Funcionario_Servicos) &&
                error.Message.Contains("nao pertence a oficina"));
            _services.Verify(repository => repository.SaveWithEmployeesAsync(
                It.IsAny<Servico>(),
                It.IsAny<IReadOnlyCollection<Funcionario_Servico>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Create_PreencheIdDoServicoEDosVinculos()
        {
            var request = CreateRequest(7, employeeId: 12);
            var entity = new Servico();
            _employees.Setup(repository => repository.ExistsInOficina(12, 7)).ReturnsAsync(true);
            _mapper.Setup(mapper => mapper.Map<Servico>(request)).Returns(entity);
            _services.Setup(repository => repository.Add(entity))
                .Callback(() => entity.Id = 31)
                .ReturnsAsync(entity);

            await CreateService().Create(request);

            Assert.Equal(31, request.Id);
            Assert.Equal(31, Assert.Single(request.Funcionario_Servicos).IdServico);
        }

        private ServicoService CreateService() =>
            new(_services.Object, _mapper.Object, _employees.Object);

        private static ServicoDTO CreateRequest(int oficinaId, int employeeId) => new()
        {
            Nome = "Troca de oleo",
            Descricao = "Troca completa",
            Valor = 150,
            Garantia = DateOnly.FromDateTime(DateTime.Today),
            IdOficina = oficinaId,
            Funcionario_Servicos = new List<Funcionario_ServicoDTO>
            {
                new() { IdFuncionario = employeeId, TempoDec = "01:00" }
            }
        };
    }
}
