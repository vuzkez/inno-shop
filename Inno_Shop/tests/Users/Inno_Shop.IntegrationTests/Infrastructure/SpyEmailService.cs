using System.Collections.Concurrent;
using Inno_Shop.Users.Application.Interfaces;

namespace Inno_Shop.IntegrationTests.Infrastructure;

/// <summary>
/// Records every confirmation / password-reset email the application tries to send.
/// The real <c>EmailService</c> talks to SMTP and swallows failures, so tests must
/// never rely on it: the spy both captures the token and keeps SMTP out of the run.
/// </summary>
public sealed class SpyEmailService : IEmailService
{
    public ConcurrentQueue<ConfirmationEmail> Confirmations { get; } = new();

    public ConcurrentQueue<PasswordResetEmail> PasswordResets { get; } = new();

    public Task SendEmailConfirmationAsync(string email, string userId, string token)
    {
        Confirmations.Enqueue(new ConfirmationEmail(email, userId, token));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string token)
    {
        PasswordResets.Enqueue(new PasswordResetEmail(email, token));
        return Task.CompletedTask;
    }
}

public sealed record ConfirmationEmail(string Email, string UserId, string Token);

public sealed record PasswordResetEmail(string Email, string Token);
