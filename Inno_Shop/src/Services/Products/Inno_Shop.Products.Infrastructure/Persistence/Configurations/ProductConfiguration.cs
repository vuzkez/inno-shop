using Inno_Shop.Products.Application.Validators;
using Inno_Shop.Products.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inno_Shop.Products.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Keeps table shape, limits and the visibility filter in one place. The limits come from
    /// <see cref="ProductRules"/>, the same constants the FluentValidation rules use.
    /// </summary>
    public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products", table =>
                table.HasCheckConstraint("CK_Products_Price", "[Price] > 0"));

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(ProductRules.MaxNameLength);

            builder.Property(p => p.Description)
                .IsRequired()
                .HasMaxLength(ProductRules.MaxDescriptionLength);

            builder.Property(p => p.Price)
                .IsRequired()
                .HasPrecision(18, ProductRules.PriceDecimalPlaces);

            builder.Property(p => p.OwnerId)
                .IsRequired()
                .HasMaxLength(ProductRules.MaxOwnerIdLength);

            builder.Property(p => p.CreatedAtUtc).IsRequired();
            builder.Property(p => p.IsAvailable).IsRequired();
            builder.Property(p => p.IsDeleted).IsRequired();
            builder.Property(p => p.IsHidden).IsRequired();

            // Filtering by owner, searching by name and sorting by price.
            builder.HasIndex(p => p.OwnerId);
            builder.HasIndex(p => p.Name);
            builder.HasIndex(p => p.Price);

            // Matches the visibility filter below, used by the catalogue queries.
            builder.HasIndex(p => new { p.IsDeleted, p.IsHidden });

            // Nothing outside the catalogue may see a deleted or hidden product.
            builder.HasQueryFilter(p => !p.IsDeleted && !p.IsHidden);
        }
    }
}
