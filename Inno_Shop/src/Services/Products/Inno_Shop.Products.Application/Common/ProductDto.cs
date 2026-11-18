using Inno_Shop.Products.Domain;

namespace Inno_Shop.Products.Application.Common
{
    /// <summary>Product projection returned by the API. Kept separate from the entity so that
    /// internal flags (<c>IsDeleted</c>, <c>IsHidden</c>) never leak into responses.</summary>
    public record ProductDto(
        Guid Id,
        string Name,
        string Description,
        decimal Price,
        bool IsAvailable,
        string OwnerId,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc)
    {
        public static ProductDto From(Product product) => new(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.IsAvailable,
            product.OwnerId,
            product.CreatedAtUtc,
            product.UpdatedAtUtc);
    }
}
