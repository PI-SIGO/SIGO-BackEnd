using Microsoft.AspNetCore.RateLimiting;
using SIGO.Controllers;
using SIGO.Security;
using Xunit;

namespace SIGO.Tests.Controllers
{
    public class RateLimitAttributeTests
    {
        [Theory]
        [InlineData(typeof(ClienteController), nameof(ClienteController.Login), RateLimitPolicies.ClienteLogin)]
        [InlineData(typeof(OficinaController), nameof(OficinaController.Login), RateLimitPolicies.OficinaLogin)]
        [InlineData(typeof(FuncionarioController), nameof(FuncionarioController.Login), RateLimitPolicies.FuncionarioLogin)]
        [InlineData(typeof(ClienteRegistrationController), nameof(ClienteRegistrationController.Register), RateLimitPolicies.PublicRegistration)]
        [InlineData(typeof(ClienteVinculoController), nameof(ClienteVinculoController.RegisterFull), RateLimitPolicies.ClientePreRegistration)]
        [InlineData(typeof(ClienteController), nameof(ClienteController.ChangePassword), RateLimitPolicies.ClientePasswordChange)]
        [InlineData(typeof(OficinaController), nameof(OficinaController.Create), RateLimitPolicies.PublicRegistration)]
        public void EndpointSensivel_DeveTerRateLimit(Type controllerType, string methodName, string policyName)
        {
            var attribute = controllerType
                .GetMethods()
                .Single(m => m.Name == methodName)
                .GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: false)
                .Cast<EnableRateLimitingAttribute>()
                .Single();

            Assert.Equal(policyName, attribute.PolicyName);
        }
    }
}
