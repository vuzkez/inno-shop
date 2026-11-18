using Inno_Shop.Contracts;
using Inno_Shop.Products.Application.Interfaces;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inno_Shop.Products.Infrastructure.Consumers
{
    /// <summary>
    /// Brings back the products hidden when the account was deactivated. Products the owner
    /// deleted themselves stay deleted, because <see cref="Domain.Product.IsDeleted"/> is untouched.
    /// </summary>
    public sealed class UserActivatedConsumer : IConsumer<UserActivatedEvent>
    {
        private readonly IApplicationDbContext _db;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<UserActivatedConsumer> _logger;

        public UserActivatedConsumer(
            IApplicationDbContext db,
            TimeProvider timeProvider,
            ILogger<UserActivatedConsumer> logger)
        {
            _db = db;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<UserActivatedEvent> context)
        {
            var userId = context.Message.UserId;

            var products = await _db.Products
                .IgnoreQueryFilters()
                .Where(product => product.OwnerId == userId && product.IsHidden)
                .ToListAsync(context.CancellationToken);

            if (products.Count == 0)
            {
                _logger.LogInformation("No products to restore for activated user {UserId}", userId);
                return;
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;

            foreach (var product in products)
            {
                product.IsHidden = false;
                product.UpdatedAtUtc = now;
            }

            await _db.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Restored {Count} products of activated user {UserId}", products.Count, userId);
        }
    }
}
