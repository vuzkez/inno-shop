using Inno_Shop.Contracts;
using Inno_Shop.Users.Application.Interfaces;
using MassTransit;

namespace Inno_Shop.Users.Infrastructure.Services;

public class UserEventPublisher : IUserEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public UserEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task DeactivateUserProductsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _publishEndpoint.Publish(
            new UserDeactivatedEvent(Guid.NewGuid(), userId, DateTime.UtcNow),
            cancellationToken);
    }

    public Task ActivateUserProductsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _publishEndpoint.Publish(
            new UserActivatedEvent(Guid.NewGuid(), userId, DateTime.UtcNow),
            cancellationToken);
    }

    public Task DeleteUserProductsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _publishEndpoint.Publish(
            new UserDeletedEvent(Guid.NewGuid(), userId, DateTime.UtcNow),
            cancellationToken);
    }
}