using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Inno_Shop.Users.Domain;
using Inno_Shop.Users.Infrastructure.Identity;
using Inno_Shop.Users.Infrastructure.Persistence;
using Inno_Shop.Users.Infrastructure.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Inno_Shop.IntegrationTests.Infrastructure;

/// <summary>
/// One fresh application per test. Containers are shared; the database is not - every test gets
/// its own database on the shared SQL Server, so tests never see each other's rows.
/// </summary>
public abstract class ApiTestBase : IAsyncLifetime
{
    private readonly IntegrationEnvironment _environment;

    protected ApiTestBase(IntegrationEnvironment environment) => _environment = environment;

    protected UsersApiFactory Factory { get; private set; } = null!;

    protected HttpClient Client { get; private set; } = null!;

    /// <summary>App log lines (warnings/errors) captured so far, useful in failure messages.</summary>
    protected string DumpLogs(int take = 15) =>
        string.Join("\n", Factory.Logs.TakeLast(take));

    public async Task InitializeAsync()
    {
        Factory = new UsersApiFactory(_environment.SqlConnectionString, _environment.RedisConnectionString);

        // Create and migrate this test's database BEFORE the API starts. Program.cs also migrates
        // at startup, but doing it here means the schema already exists, so the startup seeder
        // cannot race with database creation.
        await MigrateDatabaseAsync();

        _ = Factory.Services; // builds the host (Program.cs runs its own migrations and seeding)

        // Seeding runs on the startup thread, so ensure the roles exist deterministically.
        using (var scope = Factory.Services.CreateScope())
        {
            await DbSeeder.SeedRolesAsync(scope.ServiceProvider);
        }

        Client = Factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        Client?.Dispose();
        Factory?.Dispose();
        return Task.CompletedTask;
    }

    private async Task MigrateDatabaseAsync()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseSqlServer(Factory.SqlConnectionString)
            .Options;

        await using var db = new UsersDbContext(options);
        await db.Database.MigrateAsync();
    }

    /// <summary>Attaches a bearer token to the shared client, or clears it when null.</summary>
    protected void UseToken(string? accessToken) =>
        Client.DefaultRequestHeaders.Authorization = accessToken is null
            ? null
            : new AuthenticationHeaderValue("Bearer", accessToken);

    protected async Task<RegisterResponse> RegisterAsync(
        string email,
        string password = "Passw0rd!",
        string name = "John Doe")
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new { name, email, password });
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "registration of '{0}' should succeed but returned {1}", email, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<RegisterResponse>())!;
    }

    protected async Task<HttpResponseMessage> TryLoginAsync(string email, string password = "Passw0rd!")
    {
        UseToken(null);
        return await Client.PostAsJsonAsync("/api/auth/login", new { email, password });
    }

    protected async Task<TokensResponse> LoginAsync(string email, string password = "Passw0rd!")
    {
        var response = await TryLoginAsync(email, password);
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "login of '{0}' should succeed but returned {1}: {2}",
            email, response.StatusCode, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<TokensResponse>())!;
    }

    protected async Task<ProfileResponse> GetMeAsync(string accessToken)
    {
        UseToken(accessToken);
        var response = await Client.GetAsync("/api/users/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ProfileResponse>())!;
    }

    protected ConfirmationEmail LatestConfirmationFor(string email) =>
        Factory.Emails.Confirmations.LastOrDefault(c => c.Email == email)
        ?? throw new InvalidOperationException($"No confirmation email was sent to '{email}'.");

    /// <summary>Follows the confirmation link exactly as the recipient would.</summary>
    protected async Task ConfirmEmailAsync(ConfirmationEmail confirmation)
    {
        var url = "/api/auth/confirm-email"
                  + $"?userId={Uri.EscapeDataString(confirmation.UserId)}"
                  + $"&token={Uri.EscapeDataString(confirmation.Token)}";

        var response = await Client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "confirming the email should succeed but returned {0}", response.StatusCode);
    }

    protected async Task<RegisterResponse> RegisterConfirmedUserAsync(
        string email,
        string password = "Passw0rd!",
        string name = "John Doe")
    {
        var registration = await RegisterAsync(email, password, name);
        await ConfirmEmailAsync(LatestConfirmationFor(email));
        return registration;
    }

    /// <summary>
    /// Creates an admin directly through the real UserManager and then logs in over HTTP,
    /// so the test still exercises the genuine login + JWT path.
    /// </summary>
    protected async Task<(string UserId, string AccessToken)> CreateAdminAsync(
        string email = UsersApiFactory.AdminEmail,
        string password = UsersApiFactory.AdminPassword)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Name = "Test Administrator",
            EmailConfirmed = true,
            IsActive = true,
        };

        var created = await userManager.CreateAsync(admin, password);
        created.Succeeded.Should().BeTrue(
            because: string.Join("; ", created.Errors.Select(e => e.Description)));

        var roleAdded = await userManager.AddToRoleAsync(admin, RoleNames.Admin);
        roleAdded.Succeeded.Should().BeTrue(
            because: string.Join("; ", roleAdded.Errors.Select(e => e.Description)));

        var tokens = await LoginAsync(email, password);
        return (admin.Id, tokens.AccessToken);
    }
}
