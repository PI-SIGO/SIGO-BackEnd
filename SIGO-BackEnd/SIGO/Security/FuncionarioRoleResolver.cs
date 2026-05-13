namespace SIGO.Security
{
    public class FuncionarioRoleResolver : IFuncionarioRoleResolver
    {
        public string Resolve(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return SystemRoles.Funcionario;

            var normalizedRole = role.Trim();
            return string.Equals(normalizedRole, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase)
                ? SystemRoles.Admin
                : SystemRoles.Funcionario;
        }
    }
}
