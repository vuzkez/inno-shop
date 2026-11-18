using FluentValidation;
using MediatR;
using Inno_Shop.Products.Application.Common;
using Inno_Shop.Products.Application.Interfaces;
using Inno_Shop.Products.Application.Validators;
using Microsoft.EntityFrameworkCore;

namespace Inno_Shop.Products.Application.Features.Products.Commands
{
    public record UpdateProductCommand(
        Guid Id,
        string Name,
        string Description,
        decimal Price,
        bool IsAvailable,
        string OwnerId) : IRequest<Result<ProductDto>>;

    public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
    {
        public UpdateProductCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Product id is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required.")
                .MaximumLength(ProductRules.MaxNameLength)
                .WithMessage($"Product name must not exceed {ProductRules.MaxNameLength} characters.");

            RuleFor(x => x.Description)
                .MaximumLength(ProductRules.MaxDescriptionLength)
                .WithMessage($"Product description must not exceed {ProductRules.MaxDescriptionLength} characters.");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than zero.")
                .LessThanOrEqualTo(ProductRules.MaxPrice)
                .WithMessage($"Price must not exceed {ProductRules.MaxPrice}.")
                .Must(ProductPriceRules.HaveAtMostTwoDecimals)
                .WithMessage("Price must have at most 2 decimal places.");

            RuleFor(x => x.OwnerId)
                .NotEmpty().WithMessage("Product owner is required.");
        }
    }

    public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly TimeProvider _timeProvider;

        public UpdateProductCommandHandler(IApplicationDbContext db, TimeProvider timeProvider)
        {
            _db = db;
            _timeProvider = timeProvider;
        }

        public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (product is null)
                return Result<ProductDto>.Failure("Product not found.", ErrorType.NotFound);

            if (!string.Equals(product.OwnerId, request.OwnerId, StringComparison.Ordinal))
                return Result<ProductDto>.Failure("You can only change your own products.", ErrorType.Forbidden);

            product.Name = (request.Name ?? string.Empty).Trim();
            product.Description = (request.Description ?? string.Empty).Trim();
            product.Price = request.Price;
            product.IsAvailable = request.IsAvailable;
            product.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

            await _db.SaveChangesAsync(cancellationToken);

            return Result<ProductDto>.Success(ProductDto.From(product));
        }
    }
}
