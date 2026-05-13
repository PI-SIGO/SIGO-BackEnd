using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SIGO.Errors;
using SIGO.Exceptions;
using SIGO.Middleware;
using SIGO.Validation;
using Xunit;

namespace SIGO.Tests.Middleware
{
    public class ExceptionHandlingMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_DeveRetornarValidationProblemDetails_QuandoBusinessValidationException()
        {
            var context = CreateHttpContext();
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new BusinessValidationException(new[]
                {
                    new ValidationError("Nome", "Nome obrigatorio.")
                }),
                NullLogger<ExceptionHandlingMiddleware>.Instance);

            await middleware.InvokeAsync(context);

            var json = await ReadResponseJson(context);
            Assert.Equal(StatusCodes.Status422UnprocessableEntity, context.Response.StatusCode);
            Assert.Equal(ApiProblemDetailsFactory.ProblemContentType, context.Response.ContentType);
            Assert.Equal(ApiProblemTypes.Validation, json.GetProperty("type").GetString());
            Assert.Equal("Validacao falhou", json.GetProperty("title").GetString());
            Assert.Equal("Nome obrigatorio.", json.GetProperty("errors").GetProperty("Nome")[0].GetString());
            Assert.Equal("trace-test", json.GetProperty("traceId").GetString());
            Assert.Equal("correlation-test", json.GetProperty("correlationId").GetString());
        }

        [Fact]
        public async Task InvokeAsync_DeveRetornarNotFound_QuandoKeyNotFoundException()
        {
            var context = CreateHttpContext();
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new KeyNotFoundException("Pedido com id 10 nao encontrado."),
                NullLogger<ExceptionHandlingMiddleware>.Instance);

            await middleware.InvokeAsync(context);

            var json = await ReadResponseJson(context);
            Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
            Assert.Equal(ApiProblemTypes.NotFound, json.GetProperty("type").GetString());
            Assert.Equal("Pedido com id 10 nao encontrado.", json.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task InvokeAsync_DeveRetornarConflict_QuandoConflictException()
        {
            var context = CreateHttpContext();
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new ConflictException("Cliente ja existe."),
                NullLogger<ExceptionHandlingMiddleware>.Instance);

            await middleware.InvokeAsync(context);

            var json = await ReadResponseJson(context);
            Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
            Assert.Equal(ApiProblemTypes.Conflict, json.GetProperty("type").GetString());
            Assert.Equal("Cliente ja existe.", json.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task InvokeAsync_DeveRetornarErroInternoSemVazarMensagem_QuandoExceptionNaoTratada()
        {
            var context = CreateHttpContext();
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new InvalidOperationException("database password leaked"),
                NullLogger<ExceptionHandlingMiddleware>.Instance);

            await middleware.InvokeAsync(context);

            var json = await ReadResponseJson(context);
            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.Equal(ApiProblemTypes.InternalServerError, json.GetProperty("type").GetString());
            Assert.Equal("Ocorreu um erro inesperado.", json.GetProperty("detail").GetString());
            Assert.DoesNotContain("database password leaked", json.GetRawText(), StringComparison.OrdinalIgnoreCase);
        }

        private static DefaultHttpContext CreateHttpContext()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.Request.Path = "/api/testes";
            context.TraceIdentifier = "trace-test";
            context.Items[CorrelationIdMiddleware.ItemName] = "correlation-test";

            return context;
        }

        private static async Task<JsonElement> ReadResponseJson(DefaultHttpContext context)
        {
            context.Response.Body.Position = 0;
            using var document = await JsonDocument.ParseAsync(context.Response.Body);
            return document.RootElement.Clone();
        }
    }
}
