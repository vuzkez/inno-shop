using FluentValidation;
using MediatR;

namespace Inno_Shop.Products.Application.Behaviors
{
    /// <summary>
    /// Runs the FluentValidation rules for a request before its handler. Invalid input never
    /// reaches the handler; a <see cref="ValidationException"/> is thrown and the API turns it
    /// into a 400 ProblemDetails response.
    /// </summary>
    public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!_validators.Any())
                return await next();

            var context = new ValidationContext<TRequest>(request);

            var results = await Task.WhenAll(
                _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

            var failures = results
                .SelectMany(result => result.Errors)
                .Where(failure => failure is not null)
                .ToList();

            if (failures.Count != 0)
                throw new ValidationException(failures);

            return await next();
        }
    }
}
