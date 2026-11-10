using System;
using System.Collections.Generic;
using System.Text;
using Inno_Shop.Users.Application.Interfaces;
using Inno_Shop.Users.Application.Options;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Inno_Shop.Users.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailOptions _options;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendEmailConfirmationAsync(string email, string userId, string token)
        {
            var encodedToken = Uri.EscapeDataString(token);
            var confirmationLink = $"http://localhost:3000/confirm-email?userId={userId}&token={encodedToken}"; // replace 'http://localhost:3000' if it needs

            var body = $"<h1>Account confirmation</h1><p>Please, click on this: <a href='{confirmationLink}'>Confirm Email</a></p>";

            await SendEmailAsync(email, "Account confirmation InnoShop", body);
        }

        public async Task SendPasswordResetAsync(string email, string token)
        {
            var encodedToken = Uri.EscapeDataString(token);
            var resetLink = $"http://localhost:3000/reset-password?email={email}&token={encodedToken}";

            var body = $"<h1>Password reset</h1><p>For password reset click on this: <a href='{resetLink}'>Password reset</a></p>";

            await SendEmailAsync(email, "Password reset InnoShop", body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
            emailMessage.To.Add(new MailboxAddress("", toEmail));
            emailMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            emailMessage.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_options.UserName, _options.Password);
                await client.SendAsync(emailMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sending to email: {Email}", toEmail);
                Console.WriteLine($"Error email sender: {ex.Message}");
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}
