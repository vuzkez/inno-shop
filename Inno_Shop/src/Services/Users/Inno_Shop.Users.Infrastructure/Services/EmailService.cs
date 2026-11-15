using System.Net;
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
            var link = BuildLink(_options.ConfirmEmailUrl, ("userId", userId), ("token", token));
            var body = $"<h1>Account confirmation</h1><p>Please, click on this: <a href=\"{WebUtility.HtmlEncode(link)}\">Confirm Email</a></p>";

            await SendEmailAsync(email, "Account confirmation InnoShop", body);
        }

        public async Task SendPasswordResetAsync(string email, string token)
        {
            var link = BuildLink(_options.ResetPasswordUrl, ("email", email), ("token", token));
            var body = $"<h1>Password reset</h1><p>For password reset click on this: <a href=\"{WebUtility.HtmlEncode(link)}\">Password reset</a></p>";

            await SendEmailAsync(email, "Password reset InnoShop", body);
        }

        private static string BuildLink(string baseUrl, params (string Key, string Value)[] query)
        {
            var separator = baseUrl.Contains('?') ? '&' : '?';
            var qs = string.Join("&", query.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            return $"{baseUrl}{separator}{qs}";
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            using var client = new SmtpClient();
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
                message.To.Add(new MailboxAddress(string.Empty, toEmail));
                message.Subject = subject;
                message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

                await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_options.UserName, _options.Password);
                await client.SendAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sending email to {Email}", toEmail);
            }
            finally
            {
                if (client.IsConnected)
                    await client.DisconnectAsync(true);
            }
        }
    }
}
