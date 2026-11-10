using System;
using System.Collections.Generic;
using System.Text;

namespace Inno_Shop.Users.Application.Options
{
    public class DatabaseOptions
    {
        public const string SectionName = "ConnectionStrings";
        public string DefaultConnection { get; set; } = string.Empty;
    }
}
