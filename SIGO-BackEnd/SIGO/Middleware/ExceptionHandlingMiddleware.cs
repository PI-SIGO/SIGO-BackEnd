using Microsoft.EntityFrameworkCore;
using Npgsql;
using SIGO.Errors;
using SIGO.Exceptions;
using SIGO.Validation;

namespace SIGO.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (BusinessValidationException ex)
            {
                await ApiProblemDetailsFactory.WriteAsync(
                    context,
                    StatusCodes.Status422UnprocessableEntity,
                    detail: ex.Message,
                    type: ApiProblemTypes.Validation,
                    errors: ex.Errors,
                    cancellationToken: context.RequestAborted);
            }
            catch (ConflictException ex)
            {
                await ApiProblemDetailsFactory.WriteAsync(
                    context,
                    StatusCodes.Status409Conflict,
                    detail: ex.Message,
                    type: ApiProblemTypes.Conflict,
                    cancellationToken: context.RequestAborted);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Database concurrency conflict. TraceId={TraceId} Method={Method} Path={Path}",
                    context.TraceIdentifier,
                    context.Request.Method,
                    context.Request.Path.Value);

                await ApiProblemDetailsFactory.WriteAsync(
                    context,
                    StatusCodes.Status409Conflict,
                    detail: "O recurso foi alterado ou removido por outra operacao.",
                    type: ApiProblemTypes.Conflict,
                    cancellationToken: context.RequestAborted);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _logger.LogWarning(
                    ex,
                    "Database unique constraint conflict. TraceId={TraceId} Method={Method} Path={Path}",
                    context.TraceIdentifier,
                    context.Request.Method,
                    context.Request.Path.Value);

                await ApiProblemDetailsFactory.WriteAsync(
                    context,
                    StatusCodes.Status409Conflict,
                    detail: "Ja existe um registro com os mesmos dados unicos.",
                    type: ApiProblemTypes.Conflict,
                    cancellationToken: context.RequestAborted);
            }
            catch (KeyNotFoundException ex)
            {
                await ApiProblemDetailsFactory.WriteAsync(
                    context,
                    StatusCodes.Status404NotFound,
                    detail: string.IsNullOrWhiteSpace(ex.Message) ? null : ex.Message,
                    type: ApiProblemTypes.NotFound,
                    cancellationToken: context.RequestAborted);
            }
            catch (BadHttpRequestException)
            {
                await ApiProblemDetailsFactory.WriteAsync(
                    context,
                    StatusCodes.Status400BadRequest,
                    detail: "A requisicao HTTP e invalida.",
                    type: ApiProblemTypes.BadRequest,
                    cancellationToken: context.RequestAborted);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Authorization failure. TraceId={TraceId} Method={Method} Path={Path}",
                    context.TraceIdentifier,
                    context.Request.Method,
                    context.Request.Path.Value);

                await ApiProblemDetailsFactory.WriteAsync(
                    context,
                    StatusCodes.Status403Forbidden,
                    detail: "O usuario autenticado nao possui permissao para executar esta operacao.",
                    type: ApiProblemTypes.Forbidden,
                    cancellationToken: context.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unhandled request failure. TraceId={TraceId} Method={Method} Path={Path}",
                    context.TraceIdentifier,
                    context.Request.Method,
                    context.Request.Path.Value);

                await ApiProblemDetailsFactory.WriteAsync(
                    context,
                    StatusCodes.Status500InternalServerError,
                    detail: "Ocorreu um erro inesperado.",
                    type: ApiProblemTypes.InternalServerError,
                    cancellationToken: context.RequestAborted);
            }
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            return exception.InnerException is PostgresException postgresException &&
                postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
        }
    }
}
