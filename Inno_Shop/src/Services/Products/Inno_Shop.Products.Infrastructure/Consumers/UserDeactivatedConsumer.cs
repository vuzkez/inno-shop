using Inno_Shop.Contracts;
using Inno_Shop.Products.Application.Interfaces;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inno_Shop.Products.Infrastructure.Consumers
{
    /// <summary>
    /// Requirement 2.1: a deactivated user's products are hidden, not deleted, so activating the
    /// account brings them back untouched.
    ///
    /// Idempotent by construction: running it twice leaves the same state. RabbitMQ delivers
    /// at-least-once, so this matters.
    /// </summary>
    public sealed class UserDeactivatedConsumer : IConsumer<UserDeactivatedEvent>
    {
        private readonly IApplicationDbContext _db;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<UserDeactivatedConsumer> _logger;

        public UserDeactivatedConsumer(
            IApplicationDbContext db,
            TimeProvider timeProvider,
            ILogger<UserDeactivatedConsumer> logger)
        {
            _db = db;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<UserDeactivatedEvent> context)
        {
            var userId = context.Message.UserId;

            // IgnoreQueryFilters: hidden and already deleted rows must be included, otherwise
            // products hidden by an earlier event would be missed.
            var products = await _db.Products
                .IgnoreQueryFilters()
                .Where(product => product.OwnerId == userId && !product.IsHidden)
                .ToListAsync(context.CancellationToken);

            if (products.Count == 0)
            {
                _logger.LogInformation("No products to hide for deactivated user {UserId}", userId);
                return;
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;

            foreach (var product in products)
            {
                product.IsHidden = true;
                product.UpdatedAtUtc = now;
            }

            await _db.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Hid {Count} products of deactivated user {UserId}", products.Count, userId);
        }
    }
}
