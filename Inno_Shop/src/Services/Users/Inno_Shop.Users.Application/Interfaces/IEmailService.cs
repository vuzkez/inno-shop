using System;
using System.Collections.Generic;
using System.Text;

namespace Inno_Shop.Users.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailConfirmationAsync(string email, string userId, string token);
        Task SendPasswordResetAsync(string email, string token);
    }
}
