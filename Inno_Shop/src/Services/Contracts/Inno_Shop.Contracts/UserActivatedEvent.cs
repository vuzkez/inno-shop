using System;
using System.Collections.Generic;
using System.Text;

namespace Inno_Shop.Contracts
{
    public record UserActivatedEvent(Guid EventId, string UserId, DateTime OccurredAtUtc);
}
