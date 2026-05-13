using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SIGO.Middleware;
using SIGO.Objects.Contracts;
using SIGO.Validation;

namespace SIGO.Errors
{
    public static class ApiProblemDetailsFactory
    {
        private const string ProblemJsonContentType = "application/problem+json";

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public static async Task WriteAsync(
            HttpContext context,
            int statusCode,
            string? title = null,
            string? detail = null,
            string? type = null,
            object? errors = null,
            CancellationToken cancellationToken = default)
        {
            if (context.Response.HasStarted)
                return;

            var problemDetails = Create(context, statusCode, title, detail, type, errors);

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = ProblemJsonContentType;

            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                problemDetails,
                problemDetails.GetType(),
                JsonOptions,
                cancellationToken);
        }

        public static ProblemDetails Create(
            HttpContext context,
            int statusCode,
            string? title = null,
            string? detail = null,
            string? type = null,
            object? errors = null)
        {
            if (TryNormalizeValidationErrors(errors, out var validationErrors))
                return CreateValidation(context, validationErrors, statusCode, title, detail, type);

            var metadata = GetMetadata(statusCode);
            var problemDetails = new ProblemDetails
            {
                Type = type ?? metadata.Type,
                Title = title ?? metadata.Title,
                Status = statusCode,
                Detail = detail ?? metadata.Detail,
                Instance = GetInstance(context)
            };

            AddCommonExtensions(problemDetails, context);

            if (errors is not null)
                problemDetails.Extensions["errors"] = errors;

            return problemDetails;
        }

        public static ProblemDetails CreateValidation(
            HttpContext context,
            object errors,
            int statusCode = StatusCodes.Status422UnprocessableEntity,
            string? title = null,
            string? detail = null,
            string? type = null)
        {
            if (!TryNormalizeValidationErrors(errors, out var validationErrors))
                validationErrors = new Dictionary<string, string[]>();

            return CreateValidation(context, validationErrors, statusCode, title, detail, type);
        }

        public static ProblemDetails CreateFromErrorObject(
            HttpContext context,
            int statusCode,
            object? value)
        {
            if (value is ProblemDetails existingProblemDetails)
            {
                var metadata = GetMetadata(statusCode);
                existingProblemDetails.Status ??= statusCode;
                existingProblemDetails.Type ??= metadata.Type;
                existingProblemDetails.Title ??= metadata.Title;
                existingProblemDetails.Detail ??= metadata.Detail;
                existingProblemDetails.Instance ??= GetInstance(context);
                AddCommonExtensions(existingProblemDetails, context);

                return existingProblemDetails;
            }

            return Create(
                context,
                statusCode,
                detail: ExtractDetail(value),
                errors: ExtractErrors(value));
        }

        public static bool HasValidationErrors(object? value)
        {
            var errors = ExtractErrors(value) ?? value;
            return TryNormalizeValidationErrors(errors, out var validationErrors) &&
                validationErrors.Count > 0;
        }

        public static string ProblemContentType => ProblemJsonContentType;

        private static ProblemDetails CreateValidation(
            HttpContext context,
            IDictionary<string, string[]> errors,
            int statusCode,
            string? title,
            string? detail,
            string? type)
        {
            var validationProblemDetails = new ValidationProblemDetails(errors)
            {
                Type = type ?? ApiProblemTypes.Validation,
                Title = title ?? "Validacao falhou",
                Status = statusCode,
                Detail = detail ?? "Um ou mais campos possuem valores invalidos.",
                Instance = GetInstance(context)
            };

            AddCommonExtensions(validationProblemDetails, context);

            return validationProblemDetails;
        }

        private static void AddCommonExtensions(ProblemDetails problemDetails, HttpContext context)
        {
            problemDetails.Extensions["traceId"] = context.TraceIdentifier;

            if (context.Items.TryGetValue(CorrelationIdMiddleware.ItemName, out var correlationId) &&
                correlationId is string correlationIdValue &&
                !string.IsNullOrWhiteSpace(correlationIdValue))
            {
                problemDetails.Extensions["correlationId"] = correlationIdValue;
            }
        }

        private static bool TryNormalizeValidationErrors(
            object? errors,
            out IDictionary<string, string[]> validationErrors)
        {
            validationErrors = new Dictionary<string, string[]>();

            if (errors is null)
                return false;

            if (errors is IDictionary<string, string[]> writableDictionary)
            {
                validationErrors = writableDictionary;
                return validationErrors.Count > 0;
            }

            if (errors is IReadOnlyDictionary<string, string[]> readOnlyDictionary)
            {
                validationErrors = readOnlyDictionary.ToDictionary(
                    item => item.Key,
                    item => item.Value);
                return validationErrors.Count > 0;
            }

            if (errors is ModelStateDictionary modelState)
            {
                validationErrors = modelState
                    .Where(item => item.Value?.Errors.Count > 0)
                    .ToDictionary(
                        item => item.Key,
                        item => item.Value!.Errors
                            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                                ? "Valor invalido."
                                : error.ErrorMessage)
                            .ToArray());

                return validationErrors.Count > 0;
            }

            if (errors is IEnumerable<ValidationError> validationErrorList)
            {
                validationErrors = validationErrorList
                    .GroupBy(error => string.IsNullOrWhiteSpace(error.Field) ? "request" : error.Field)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .Select(error => string.IsNullOrWhiteSpace(error.Message)
                                ? "Valor invalido."
                                : error.Message)
                            .ToArray());

                return validationErrors.Count > 0;
            }

            if (errors is SerializableError serializableError)
            {
                validationErrors = serializableError.ToDictionary(
                    item => item.Key,
                    item => NormalizeSerializableErrorValue(item.Value));

                return validationErrors.Count > 0;
            }

            if (errors is IDictionary<string, object> objectDictionary)
            {
                validationErrors = objectDictionary.ToDictionary(
                    item => item.Key,
                    item => NormalizeSerializableErrorValue(item.Value));

                return validationErrors.Count > 0;
            }

            return false;
        }

        private static string[] NormalizeSerializableErrorValue(object? value)
        {
            if (value is null)
                return Array.Empty<string>();

            if (value is string message)
                return new[] { message };

            if (value is string[] messages)
                return messages;

            if (value is IEnumerable<string> enumerableMessages)
                return enumerableMessages.ToArray();

            if (value is IEnumerable<object> enumerableObjects)
            {
                return enumerableObjects
                    .Select(item => item?.ToString())
                    .Where(message => !string.IsNullOrWhiteSpace(message))
                    .Cast<string>()
                    .ToArray();
            }

            var text = value.ToString();
            return string.IsNullOrWhiteSpace(text) ? Array.Empty<string>() : new[] { text };
        }

        private static string? ExtractDetail(object? value)
        {
            return value switch
            {
                null => null,
                string message when !string.IsNullOrWhiteSpace(message) => message,
                Response response when !string.IsNullOrWhiteSpace(response.Message) => response.Message,
                ProblemDetails problemDetails when !string.IsNullOrWhiteSpace(problemDetails.Detail) => problemDetails.Detail,
                ProblemDetails problemDetails when !string.IsNullOrWhiteSpace(problemDetails.Title) => problemDetails.Title,
                _ => TryGetStringProperty(value, "Message")
            };
        }

        private static object? ExtractErrors(object? value)
        {
            if (value is null)
                return null;

            if (value is Response response)
                return response.Data;

            if (value is ValidationProblemDetails validationProblemDetails)
                return validationProblemDetails.Errors;

            if (value is ProblemDetails problemDetails &&
                problemDetails.Extensions.TryGetValue("errors", out var errors))
            {
                return errors;
            }

            return TryGetPropertyValue(value, "Errors");
        }

        private static string? TryGetStringProperty(object value, string propertyName)
        {
            return TryGetPropertyValue(value, propertyName) as string;
        }

        private static object? TryGetPropertyValue(object value, string propertyName)
        {
            var property = value
                .GetType()
                .GetProperty(
                    propertyName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            return property?.GetValue(value);
        }

        private static string GetInstance(HttpContext context)
        {
            return context.Request.Path.HasValue ? context.Request.Path.Value! : "/";
        }

        private static (string Type, string Title, string Detail) GetMetadata(int statusCode)
        {
            return statusCode switch
            {
                StatusCodes.Status400BadRequest => (
                    ApiProblemTypes.BadRequest,
                    "Requisicao invalida",
                    "A requisicao nao pode ser processada."),
                StatusCodes.Status401Unauthorized => (
                    ApiProblemTypes.Unauthorized,
                    "Nao autenticado",
                    "Autenticacao e obrigatoria para acessar este recurso."),
                StatusCodes.Status403Forbidden => (
                    ApiProblemTypes.Forbidden,
                    "Acesso negado",
                    "O usuario autenticado nao possui permissao para acessar este recurso."),
                StatusCodes.Status404NotFound => (
                    ApiProblemTypes.NotFound,
                    "Recurso nao encontrado",
                    "O recurso solicitado nao foi encontrado."),
                StatusCodes.Status409Conflict => (
                    ApiProblemTypes.Conflict,
                    "Conflito",
                    "A requisicao conflita com o estado atual do recurso."),
                StatusCodes.Status422UnprocessableEntity => (
                    ApiProblemTypes.Validation,
                    "Validacao falhou",
                    "Um ou mais campos possuem valores invalidos."),
                StatusCodes.Status429TooManyRequests => (
                    ApiProblemTypes.RateLimit,
                    "Muitas requisicoes",
                    "Limite de requisicoes excedido. Tente novamente mais tarde."),
                >= StatusCodes.Status500InternalServerError => (
                    ApiProblemTypes.InternalServerError,
                    "Erro interno",
                    "Ocorreu um erro inesperado."),
                _ => (
                    ApiProblemTypes.HttpError,
                    "Erro HTTP",
                    "A requisicao nao pode ser concluida.")
            };
        }
    }
}
