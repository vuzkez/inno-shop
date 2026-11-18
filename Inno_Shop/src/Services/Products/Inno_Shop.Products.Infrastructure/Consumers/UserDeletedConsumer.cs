using Inno_Shop.Contracts;
using Inno_Shop.Products.Application.Interfaces;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inno_Shop.Products.Infrastructure.Consumers
{
    /// <summary>
    /// The account is gone for good, so its products are deleted permanently - this is the one
    /// place that removes rows instead of flagging them.
    ///
    /// IgnoreQueryFilters is essential here: hidden and soft-deleted products must go too,
    /// otherwise they would stay in the database forever with no owner.
    /// Idempotent: a second delivery finds nothing and does nothing.
    /// </summary>
    public sealed class UserDeletedConsumer : IConsumer<UserDeletedEvent>
    {
        private readonly IApplicationDbContext _db;
        private readonly ILogger<UserDeletedConsumer> _logger;

        public UserDeletedConsumer(IApplicationDbContext db, ILogger<UserDeletedConsumer> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<UserDeletedEvent> context)
        {
            var userId = context.Message.UserId;

            var products = await _db.Products
                .IgnoreQueryFilters()
                .Where(product => product.OwnerId == userId)
                .ToListAsync(context.CancellationToken);

            if (products.Count == 0)
            {
                _logger.LogInformation("No products to delete for removed user {UserId}", userId);
                return;
            }

            _db.Products.RemoveRange(products);
            await _db.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Permanently deleted {Count} products of removed user {UserId}", products.Count, userId);
        }
    }
}
