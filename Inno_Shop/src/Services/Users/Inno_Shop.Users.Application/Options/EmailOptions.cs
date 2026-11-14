using System;
using System.Collections.Generic;
using System.Text;

namespace Inno_Shop.Users.Application.Options
{
    public class EmailOptions
    {
        public const string SectionName = "EmailSettings";
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public string ConfirmEmailUrl { get; set; } = "http://localhost:5145/api/auth/confirm-email";
        public string ResetPasswordUrl { get; set; } = "http://localhost:3000/reset-password";
    }
}
