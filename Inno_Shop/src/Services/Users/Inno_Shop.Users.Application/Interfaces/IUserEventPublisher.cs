using System;
using System.Collections.Generic;
using System.Text;

namespace Inno_Shop.Users.Application.Interfaces
{
    public interface IUserEventPublisher
    {
        Task DeactivateUserProductsAsync(string userId, CancellationToken cancellationToken = default);
        Task ActivateUserProductsAsync(string userId, CancellationToken cancellationToken = default);
        Task DeleteUserProductsAsync(string userId, CancellationToken cancellationToken = default);
    }
}
