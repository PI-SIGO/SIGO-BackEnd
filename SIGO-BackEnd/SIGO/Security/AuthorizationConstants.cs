namespace SIGO.Security
{
    public static class SystemRoles
    {
        public const string Admin = "Admin";
        public const string Funcionario = "Funcionario";
        public const string Oficina = "Oficina";
        public const string Cliente = "Cliente";
    }

    public static class AuthorizationPolicies
    {
        public const string FullAccess = "FullAccess";
        public const string OperationalAccess = "OperationalAccess";
        public const string SelfServiceAccess = "SelfServiceAccess";
    }

    public static class CustomClaimTypes
    {
        public const string OficinaId = "oficina_id";
        public const string TokenVersion = "token_version";
    }

    public static class RateLimitPolicies
    {
        public const string ClienteLogin = "ClienteLogin";
        public const string OficinaLogin = "OficinaLogin";
        public const string FuncionarioLogin = "FuncionarioLogin";
        public const string PublicRegistration = "PublicRegistration";
        public const string ClientePreRegistration = "ClientePreRegistration";
        public const string ClientePasswordChange = "ClientePasswordChange";
        public const string CepLookup = "CepLookup";
    }
}
