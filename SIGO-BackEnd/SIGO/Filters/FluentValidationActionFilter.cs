using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SIGO.Errors;

namespace SIGO.Filters
{
    public sealed class FluentValidationActionFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            foreach (var argument in context.ActionArguments.Values.Where(value => value is not null))
            {
                var argumentType = argument!.GetType();
                var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
                if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                    continue;

                var validationContext = new ValidationContext<object>(argument);
                var result = await validator.ValidateAsync(
                    validationContext,
                    context.HttpContext.RequestAborted);

                foreach (var error in result.Errors)
                    context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            if (context.ModelState.IsValid)
            {
                await next();
                return;
            }

            var problemDetails = ApiProblemDetailsFactory.CreateValidation(
                context.HttpContext,
                context.ModelState);

            context.Result = new UnprocessableEntityObjectResult(problemDetails)
            {
                DeclaredType = problemDetails.GetType()
            };
        }
    }
}
