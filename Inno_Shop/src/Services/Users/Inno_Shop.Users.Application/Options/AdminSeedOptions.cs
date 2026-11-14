using System;
using System.Collections.Generic;
using System.Text;

namespace Inno_Shop.Users.Application.Options
{
    public class AdminSeedOptions
    {
        public const string SectionName = "AdminSeed";
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Name { get; set; } = "Administrator";
    }
}
