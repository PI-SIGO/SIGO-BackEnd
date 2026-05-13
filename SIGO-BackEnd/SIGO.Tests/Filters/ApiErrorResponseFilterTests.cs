using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using SIGO.Errors;
using SIGO.Filters;
using SIGO.Objects.Contracts;
using SIGO.Validation;
using Xunit;

namespace SIGO.Tests.Filters
{
    public class ApiErrorResponseFilterTests
    {
        [Fact]
        public async Task OnResultExecutionAsync_DeveNormalizarNotFoundObjectResult()
        {
            var context = CreateContext(new NotFoundObjectResult(new { Message = "Cliente nao encontrado." }));
            var filter = new ApiErrorResponseFilter();

            await filter.OnResultExecutionAsync(context, () => CreateExecutedContext(context));

            var objectResult = Assert.IsType<ObjectResult>(context.Result);
            var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
            Assert.Equal(ApiProblemTypes.NotFound, problemDetails.Type);
            Assert.Equal("Cliente nao encontrado.", problemDetails.Detail);
            Assert.Contains(ApiProblemDetailsFactory.ProblemContentType, objectResult.ContentTypes);
        }

        [Fact]
        public async Task OnResultExecutionAsync_DeveNormalizarResponseComErrosDeValidacao()
        {
            var response = new Response
            {
                Code = ResponseEnum.INVALID,
                Message = "Dados invalidos.",
                Data = new[]
                {
                    new ValidationError("Nome", "Nome obrigatorio.")
                }
            };
            var context = CreateContext(new BadRequestObjectResult(response));
            var filter = new ApiErrorResponseFilter();

            await filter.OnResultExecutionAsync(context, () => CreateExecutedContext(context));

            var objectResult = Assert.IsType<ObjectResult>(context.Result);
            var problemDetails = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
            Assert.Equal(StatusCodes.Status422UnprocessableEntity, objectResult.StatusCode);
            Assert.Equal(ApiProblemTypes.Validation, problemDetails.Type);
            Assert.Equal("Dados invalidos.", problemDetails.Detail);
            Assert.Equal("Nome obrigatorio.", problemDetails.Errors["Nome"].Single());
        }

        private static ResultExecutingContext CreateContext(IActionResult result)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/api/clientes/1";
            httpContext.TraceIdentifier = "trace-test";
            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor());

            return new ResultExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                result,
                new object());
        }

        private static Task<ResultExecutedContext> CreateExecutedContext(ResultExecutingContext context)
        {
            var actionContext = new ActionContext(
                context.HttpContext,
                context.RouteData,
                context.ActionDescriptor,
                context.ModelState);

            return Task.FromResult(new ResultExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                context.Result,
                new object()));
        }
    }
}
