using Microsoft.AspNetCore.Mvc;

namespace SIGO.Errors;

internal static class ControllerProblemExtensions
{
    internal static ObjectResult ApiProblem(
        this ControllerBase controller,
        int statusCode,
        string? detail = null)
    {
        var httpContext = controller.HttpContext ?? new DefaultHttpContext();
        var problemDetails = ApiProblemDetailsFactory.Create(
            httpContext,
            statusCode,
            detail: detail);

        return CreateResult(problemDetails, statusCode);
    }

    internal static ObjectResult ApiValidationProblem(
        this ControllerBase controller,
        int statusCode,
        string field,
        string message,
        string? detail = null)
    {
        var httpContext = controller.HttpContext ?? new DefaultHttpContext();
        var errors = new Dictionary<string, string[]>
        {
            [field] = new[] { message }
        };
        var problemDetails = ApiProblemDetailsFactory.CreateValidation(
            httpContext,
            errors,
            statusCode,
            detail: detail);

        return CreateResult(problemDetails, statusCode);
    }

    private static ObjectResult CreateResult(ProblemDetails problemDetails, int statusCode)
    {
        var result = new ObjectResult(problemDetails)
        {
            StatusCode = statusCode,
            DeclaredType = problemDetails.GetType()
        };
        result.ContentTypes.Add(ApiProblemDetailsFactory.ProblemContentType);

        return result;
    }
}
