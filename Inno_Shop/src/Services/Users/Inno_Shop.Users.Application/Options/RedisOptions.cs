using System;
using System.Collections.Generic;
using System.Text;

namespace Inno_Shop.Users.Application.Options
{
    public class RedisOptions
    {
        public const string SectionName = "RedisSettings";
        public string ConnectionString { get; set; } = string.Empty;
    }
}
