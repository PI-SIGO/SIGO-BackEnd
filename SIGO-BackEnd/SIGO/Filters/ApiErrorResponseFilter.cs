using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SIGO.Errors;

namespace SIGO.Filters
{
    public sealed class ApiErrorResponseFilter : IAsyncResultFilter
    {
        public async Task OnResultExecutionAsync(
            ResultExecutingContext context,
            ResultExecutionDelegate next)
        {
            if (TryGetErrorStatusCode(context.Result, out var statusCode))
            {
                var value = context.Result is ObjectResult objectResult ? objectResult.Value : null;
                statusCode = NormalizeStatusCode(statusCode, value);
                var problemDetails = ApiProblemDetailsFactory.CreateFromErrorObject(
                    context.HttpContext,
                    statusCode,
                    value);

                var normalizedResult = new ObjectResult(problemDetails)
                {
                    StatusCode = statusCode,
                    DeclaredType = problemDetails.GetType()
                };
                normalizedResult.ContentTypes.Add(ApiProblemDetailsFactory.ProblemContentType);

                context.Result = normalizedResult;
            }

            await next();
        }

        private static bool TryGetErrorStatusCode(IActionResult result, out int statusCode)
        {
            statusCode = result switch
            {
                ObjectResult objectResult => objectResult.StatusCode ?? StatusCodes.Status200OK,
                StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
                JsonResult jsonResult when jsonResult.StatusCode.HasValue => jsonResult.StatusCode.Value,
                _ => StatusCodes.Status200OK
            };

            return statusCode is >= StatusCodes.Status400BadRequest and < 600;
        }

        private static int NormalizeStatusCode(int statusCode, object? value)
        {
            if (statusCode == StatusCodes.Status400BadRequest &&
                ApiProblemDetailsFactory.HasValidationErrors(value))
            {
                return StatusCodes.Status422UnprocessableEntity;
            }

            return statusCode;
        }
    }
}
