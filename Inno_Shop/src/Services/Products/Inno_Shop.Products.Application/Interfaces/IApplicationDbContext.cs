using Microsoft.EntityFrameworkCore;
using Inno_Shop.Products.Domain;

namespace Inno_Shop.Products.Application.Interfaces
{
    /// <summary>
    /// The only persistence abstraction the application layer knows about.
    /// Handlers query <see cref="Products"/> and call <see cref="SaveChangesAsync"/>; there is no
    /// repository layer, because for this service a repository would only duplicate EF Core.
    /// </summary>
    public interface IApplicationDbContext
    {
        DbSet<Product> Products { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
