using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Inno_Shop.Users.Api.Common
{
    public class ValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var errors = new Dictionary<string, List<string>>();

            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument is null)
                    continue;

                var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
                if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                    continue;

                var result = await validator.ValidateAsync(
                    new ValidationContext<object>(argument),
                    context.HttpContext.RequestAborted);

                foreach (var failure in result.Errors)
                {
                    var key = string.IsNullOrEmpty(failure.PropertyName) ? "request" : failure.PropertyName;
                    if (!errors.TryGetValue(key, out var list))
                        errors[key] = list = new List<string>();
                    list.Add(failure.ErrorMessage);
                }
            }

            if (errors.Count > 0)
            {
                var details = new ValidationProblemDetails(errors.ToDictionary(e => e.Key, e => e.Value.ToArray()))
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred."
                };
                context.Result = new BadRequestObjectResult(details);
                return;
            }

            await next();
        }
    }
}
