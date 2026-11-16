using System;
using System.Collections.Generic;
using System.Text;

namespace Inno_Shop.Users.Application.Options
{
    public class RabbitMqOptions
    {
        public const string SectionName = "RabbitMqSettings";
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string VirtualHost { get; set; } = "/";
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
