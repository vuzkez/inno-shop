using System.Collections.Concurrent;
using Inno_Shop.Users.Application.Interfaces;

namespace Inno_Shop.IntegrationTests.Infrastructure;

/// <summary>
/// Captures the integration events that would otherwise be published to RabbitMQ.
/// This is the contract the future Products microservice has to consume, so the
/// assertion "deactivating a user publishes UserDeactivatedEvent" is worth keeping.
/// </summary>
public sealed class SpyEventPublisher : IUserEventPublisher
{
    public ConcurrentQueue<string> DeactivatedUsers { get; } = new();

    public ConcurrentQueue<string> ActivatedUsers { get; } = new();

    public ConcurrentQueue<string> DeletedUsers { get; } = new();

    public Task DeactivateUserProductsAsync(string userId, CancellationToken cancellationToken = default)
    {
        DeactivatedUsers.Enqueue(userId);
        return Task.CompletedTask;
    }

    public Task ActivateUserProductsAsync(string userId, CancellationToken cancellationToken = default)
    {
        ActivatedUsers.Enqueue(userId);
        return Task.CompletedTask;
    }

    public Task DeleteUserProductsAsync(string userId, CancellationToken cancellationToken = default)
    {
        DeletedUsers.Enqueue(userId);
        return Task.CompletedTask;
    }
}
