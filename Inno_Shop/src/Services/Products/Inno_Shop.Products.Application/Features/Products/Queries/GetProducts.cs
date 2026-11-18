using FluentValidation;
using MediatR;
using Inno_Shop.Products.Application.Common;
using Inno_Shop.Products.Application.Interfaces;
using Inno_Shop.Products.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inno_Shop.Products.Application.Features.Products.Queries
{
    public record GetProductsQuery(
        string? Search = null,
        decimal? MinPrice = null,
        decimal? MaxPrice = null,
        bool? IsAvailable = null,
        string? SortBy = null,
        bool Descending = true,
        int Page = 1,
        int PageSize = 20) : IRequest<PagedResult<ProductDto>>;

    public sealed class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
    {
        private static readonly string[] AllowedSortFields = { "name", "price", "createdat" };

        public GetProductsQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");
            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");

            RuleFor(x => x.Search)
                .MaximumLength(200).WithMessage("Search must not exceed 200 characters.");

            RuleFor(x => x.MinPrice)
                .GreaterThanOrEqualTo(0).WithMessage("MinPrice must not be negative.")
                .When(x => x.MinPrice.HasValue);

            RuleFor(x => x.MaxPrice)
                .GreaterThanOrEqualTo(0).WithMessage("MaxPrice must not be negative.")
                .When(x => x.MaxPrice.HasValue);

            RuleFor(x => x)
                .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
                .WithMessage("MinPrice must be less than or equal to MaxPrice.");

            RuleFor(x => x.SortBy)
                .Must(sortBy => AllowedSortFields.Contains(sortBy!.ToLowerInvariant()))
                .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortFields)}.")
                .When(x => !string.IsNullOrWhiteSpace(x.SortBy));
        }
    }

    public sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
    {
        private readonly IApplicationDbContext _db;

        public GetProductsQueryHandler(IApplicationDbContext db) => _db = db;

        public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
        {
            // Deleted and hidden products are filtered out by the global query filter in the DbContext.
            var query = _db.Products.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
            }

            if (request.MinPrice.HasValue)
                query = query.Where(p => p.Price >= request.MinPrice.Value);

            if (request.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= request.MaxPrice.Value);

            if (request.IsAvailable.HasValue)
                query = query.Where(p => p.IsAvailable == request.IsAvailable.Value);

            query = ApplySorting(query, request.SortBy, request.Descending);

            var total = await query.CountAsync(cancellationToken);

            var page = request.Page < 1 ? 1 : request.Page;
            int pageSize = request.PageSize;

            if (pageSize < 1 || pageSize > 100)
                pageSize = 20;

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<ProductDto>(
                products.Select(ProductDto.From).ToList(),
                page,
                pageSize,
                total);
        }

        private static IQueryable<Product> ApplySorting(IQueryable<Product> query, string? sortBy, bool descending) =>
            sortBy?.ToLowerInvariant() switch
            {
                "name" => descending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "price" => descending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                _ => descending
                    ? query.OrderByDescending(p => p.CreatedAtUtc)
                    : query.OrderBy(p => p.CreatedAtUtc),
            };
    }
}
