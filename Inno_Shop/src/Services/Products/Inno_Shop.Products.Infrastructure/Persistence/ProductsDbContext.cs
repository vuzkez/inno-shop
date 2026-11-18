using Inno_Shop.Products.Application.Interfaces;
using Inno_Shop.Products.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inno_Shop.Products.Infrastructure.Persistence
{
    public class ProductsDbContext : DbContext, IApplicationDbContext
    {
        public ProductsDbContext(DbContextOptions<ProductsDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products => Set<Product>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductsDbContext).Assembly);
        }
    }
}
