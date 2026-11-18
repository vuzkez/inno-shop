using FluentValidation;
using MediatR;
using Inno_Shop.Products.Application.Common;
using Inno_Shop.Products.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inno_Shop.Products.Application.Features.Products.Queries
{
    public record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductDto>>;

    public sealed class GetProductByIdQueryValidator : AbstractValidator<GetProductByIdQuery>
    {
        public GetProductByIdQueryValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Product id is required.");
        }
    }

    public sealed class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
    {
        private readonly IApplicationDbContext _db;

        public GetProductByIdQueryHandler(IApplicationDbContext db) => _db = db;

        public async Task<Result<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            var product = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            return product is null
                ? Result<ProductDto>.Failure("Product not found.", ErrorType.NotFound)
                : Result<ProductDto>.Success(ProductDto.From(product));
        }
    }
}
