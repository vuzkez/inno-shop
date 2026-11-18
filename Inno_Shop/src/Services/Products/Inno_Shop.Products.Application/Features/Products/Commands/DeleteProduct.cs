using FluentValidation;
using MediatR;
using Inno_Shop.Products.Application.Common;
using Inno_Shop.Products.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inno_Shop.Products.Application.Features.Products.Commands
{
    /// <summary>Soft delete: the row stays, but the global query filter hides it from now on.</summary>
    public record DeleteProductCommand(Guid Id, string OwnerId) : IRequest<Result>;

    public sealed class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
    {
        public DeleteProductCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Product id is required.");
            RuleFor(x => x.OwnerId).NotEmpty().WithMessage("Product owner is required.");
        }
    }

    public sealed class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
    {
        private readonly IApplicationDbContext _db;
        private readonly TimeProvider _timeProvider;

        public DeleteProductCommandHandler(IApplicationDbContext db, TimeProvider timeProvider)
        {
            _db = db;
            _timeProvider = timeProvider;
        }

        public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (product is null)
                return Result.Failure("Product not found.", ErrorType.NotFound);

            if (!string.Equals(product.OwnerId, request.OwnerId, StringComparison.Ordinal))
                return Result.Failure("You can only delete your own products.", ErrorType.Forbidden);

            product.IsDeleted = true;
            product.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

            await _db.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
