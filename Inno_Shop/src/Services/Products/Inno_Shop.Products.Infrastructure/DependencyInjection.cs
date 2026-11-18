using Inno_Shop.Products.Application.Interfaces;
using Inno_Shop.Products.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Inno_Shop.Products.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Database Error: Connection string 'DefaultConnection' is missing.");

            services.AddDbContext<ProductsDbContext>(options => options.UseSqlServer(connectionString));

            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ProductsDbContext>());

            return services;
        }
    }
}
