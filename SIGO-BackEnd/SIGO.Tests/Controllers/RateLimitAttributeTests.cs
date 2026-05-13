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
        [InlineData(typeof(ClienteController), nameof(ClienteController.Post), RateLimitPolicies.PublicRegistration)]
        [InlineData(typeof(OficinaController), nameof(OficinaController.Create), RateLimitPolicies.PublicRegistration)]
        public void EndpointPublico_DeveTerRateLimit(Type controllerType, string methodName, string policyName)
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
