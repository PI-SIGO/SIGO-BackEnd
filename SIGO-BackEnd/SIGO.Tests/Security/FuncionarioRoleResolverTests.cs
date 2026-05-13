using SIGO.Security;
using Xunit;

namespace SIGO.Tests.Security
{
    public class FuncionarioRoleResolverTests
    {
        [Fact]
        public void Resolve_NaoDeveTransformarCargoAdminEmAdminGlobal()
        {
            var resolver = new FuncionarioRoleResolver();

            var role = resolver.Resolve("ADMINISTRADOR");

            Assert.Equal(SystemRoles.Funcionario, role);
        }

        [Fact]
        public void Resolve_DeveAceitarSomenteRoleAdminPersistida()
        {
            var resolver = new FuncionarioRoleResolver();

            var role = resolver.Resolve(SystemRoles.Admin);

            Assert.Equal(SystemRoles.Admin, role);
        }
    }
}
