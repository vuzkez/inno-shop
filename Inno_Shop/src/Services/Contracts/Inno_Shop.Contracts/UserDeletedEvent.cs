using System;
using System.Collections.Generic;
using System.Text;

namespace Inno_Shop.Contracts
{
    public record UserDeletedEvent(Guid EventId, string UserId, DateTime OccurredAtUtc);
}
