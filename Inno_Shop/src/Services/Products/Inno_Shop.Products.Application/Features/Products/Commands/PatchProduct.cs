using FluentValidation;
using MediatR;
using Inno_Shop.Products.Application.Common;
using Inno_Shop.Products.Application.Interfaces;
using Inno_Shop.Products.Application.Validators;
using Microsoft.EntityFrameworkCore;

namespace Inno_Shop.Products.Application.Features.Products.Commands
{
    /// <summary>
    /// Partial update: only the fields that are present in the request are applied.
    /// A null field means "leave as is", which is why the parameters are nullable.
    /// </summary>
    public record PatchProductCommand(
        Guid Id,
        string OwnerId,
        string? Name = null,
        string? Description = null,
        decimal? Price = null,
        bool? IsAvailable = null) : IRequest<Result<ProductDto>>;

    public sealed class PatchProductCommandValidator : AbstractValidator<PatchProductCommand>
    {
        public PatchProductCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Product id is required.");

            RuleFor(x => x.OwnerId)
                .NotEmpty().WithMessage("Product owner is required.");

            RuleFor(x => x)
                .Must(x => x.Name is not null
                           || x.Description is not null
                           || x.Price.HasValue
                           || x.IsAvailable.HasValue)
                .WithMessage("At least one field must be provided.");

            When(x => x.Name is not null, () =>
                RuleFor(x => x.Name!)
                    .NotEmpty().WithMessage("Product name is required.")
                    .MaximumLength(ProductRules.MaxNameLength)
                    .WithMessage($"Product name must not exceed {ProductRules.MaxNameLength} characters."));

            When(x => x.Description is not null, () =>
                RuleFor(x => x.Description!)
                    .MaximumLength(ProductRules.MaxDescriptionLength)
                    .WithMessage($"Product description must not exceed {ProductRules.MaxDescriptionLength} characters."));

            When(x => x.Price.HasValue, () =>
                RuleFor(x => x.Price!.Value)
                    .GreaterThan(0).WithMessage("Price must be greater than zero.")
                    .LessThanOrEqualTo(ProductRules.MaxPrice)
                    .WithMessage($"Price must not exceed {ProductRules.MaxPrice}.")
                    .Must(ProductPriceRules.HaveAtMostTwoDecimals)
                    .WithMessage("Price must have at most 2 decimal places."));
        }
    }

    public sealed class PatchProductCommandHandler : IRequestHandler<PatchProductCommand, Result<ProductDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly TimeProvider _timeProvider;

        public PatchProductCommandHandler(IApplicationDbContext db, TimeProvider timeProvider)
        {
            _db = db;
            _timeProvider = timeProvider;
        }

        public async Task<Result<ProductDto>> Handle(PatchProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (product is null)
                return Result<ProductDto>.Failure("Product not found.", ErrorType.NotFound);

            if (!string.Equals(product.OwnerId, request.OwnerId, StringComparison.Ordinal))
                return Result<ProductDto>.Failure("You can only change your own products.", ErrorType.Forbidden);

            if (request.Name is not null)
                product.Name = request.Name.Trim();

            if (request.Description is not null)
                product.Description = request.Description.Trim();

            if (request.Price.HasValue)
                product.Price = request.Price.Value;

            if (request.IsAvailable.HasValue)
                product.IsAvailable = request.IsAvailable.Value;

            product.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

            await _db.SaveChangesAsync(cancellationToken);

            return Result<ProductDto>.Success(ProductDto.From(product));
        }
    }
}
