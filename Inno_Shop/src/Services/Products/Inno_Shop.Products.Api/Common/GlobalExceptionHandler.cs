using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Inno_Shop.Products.Api.Common
{
    /// <summary>
    /// Turns exceptions into ProblemDetails. Same shape as the Users service, so a client can
    /// handle both APIs with one parser.
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var problem = exception switch
            {
                ValidationException validation => new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed",
                    Detail = string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)),
                },
                UnauthorizedAccessException => new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                },
                OperationCanceledException => new ProblemDetails
                {
                    Status = 499,
                    Title = "Client closed request",
                },
                _ => new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected error occurred",
                    Detail = _environment.IsDevelopment() ? exception.ToString() : null,
                },
            };

            if (exception is ValidationException invalid)
            {
                problem.Extensions["errors"] = invalid.Errors
                    .Select(error => error.ErrorMessage)
                    .ToList();
            }

            if (problem.Status >= 500)
                _logger.LogError(exception, "Unhandled exception");

            problem.Instance = httpContext.Request.Path;
            problem.Extensions["traceId"] = httpContext.TraceIdentifier;

            httpContext.Response.StatusCode = problem.Status!.Value;
            await httpContext.Response.WriteAsJsonAsync(
                problem,
                options: null,
                contentType: "application/problem+json",
                cancellationToken);

            return true;
        }
    }
}
