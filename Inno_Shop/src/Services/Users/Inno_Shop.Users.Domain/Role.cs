using System;
using System.Collections.Generic;
using System.Text;

namespace Inno_Shop.Users.Domain
{
    public enum Role
    {
        Customer,
        Admin
    }
    public static class RoleNames
    {
        public const string Customer = nameof(Role.Customer);
        public const string Admin = nameof(Role.Admin);
    }
}
