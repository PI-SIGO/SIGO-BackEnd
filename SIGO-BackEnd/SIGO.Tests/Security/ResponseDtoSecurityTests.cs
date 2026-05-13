using SIGO.Objects.Dtos.Entities;
using System.Text.Json;
using Xunit;

namespace SIGO.Tests.Security
{
    public class ResponseDtoSecurityTests
    {
        [Theory]
        [MemberData(nameof(ResponseDtos))]
        public void ResponseDto_NaoDeveSerializarCampoSenha(object dto)
        {
            var json = JsonSerializer.Serialize(dto);

            Assert.DoesNotContain("senha", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        }

        public static IEnumerable<object[]> ResponseDtos()
        {
            yield return new object[] { new ClienteDTO { Nome = "Cliente" } };
            yield return new object[] { new FuncionarioDTO { Nome = "Funcionario" } };
            yield return new object[] { new OficinaDTO { Nome = "Oficina" } };
        }
    }
}
