using FluentValidation;
using MediatR;
using Inno_Shop.Products.Application.Common;
using Inno_Shop.Products.Application.Interfaces;
using Inno_Shop.Products.Application.Validators;
using Inno_Shop.Products.Domain;

namespace Inno_Shop.Products.Application.Features.Products.Commands
{
    public record CreateProductCommand(
        string Name,
        string Description,
        decimal Price,
        bool IsAvailable,
        string OwnerId) : IRequest<Result<ProductDto>>;

    public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
    {
        public CreateProductCommandValidator()
        {
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

    public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<ProductDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly TimeProvider _timeProvider;

        public CreateProductCommandHandler(IApplicationDbContext db, TimeProvider timeProvider)
        {
            _db = db;
            _timeProvider = timeProvider;
        }

        public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            var product = new Product
            {
                Name = (request.Name ?? string.Empty).Trim(),
                Description = (request.Description ?? string.Empty).Trim(),
                Price = request.Price,
                IsAvailable = request.IsAvailable,
                OwnerId = request.OwnerId,
                CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync(cancellationToken);

            return Result<ProductDto>.Success(ProductDto.From(product));
        }
    }
}
