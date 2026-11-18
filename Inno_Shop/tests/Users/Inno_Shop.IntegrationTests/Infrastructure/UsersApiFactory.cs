using Inno_Shop.Users.Api;
using Inno_Shop.Users.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Inno_Shop.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real Users API in memory against real infrastructure.
/// </summary>
public sealed class UsersApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@innoshop.local";
    public const string AdminPassword = "AdminPassw0rd";

    public const string JwtIssuer = "InnoShop.Users.Tests";
    public const string JwtAudience = "InnoShop.Tests";

    /// <summary>Must be at least 32 characters, which JwtOptions validation enforces.</summary>
    public const string JwtSecret = "kkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkk";

    private readonly string _redisConnectionString;

    public UsersApiFactory(string sqlConnectionString, string redisConnectionString)
    {
        DatabaseName = $"InnoShopUsers_{Guid.NewGuid():N}";

        // Same server as the shared container, but a database of this factory's own - that is how
        // tests stay isolated without cleaning anything up.
        SqlConnectionString = new SqlConnectionStringBuilder(sqlConnectionString)
        {
            InitialCatalog = DatabaseName,
        }.ConnectionString;

        _redisConnectionString = redisConnectionString;
    }

    public string DatabaseName { get; }

    public string SqlConnectionString { get; }

    public SpyEmailService Emails { get; } = new();

    public SpyEventPublisher Events { get; } = new();

    /// <summary>Warning/error log lines captured during the test, with exception messages.</summary>
    public ConcurrentQueue<string> Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.AddProvider(new CapturingLoggerProvider(Logs)));
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(TestSettings()));
        builder.ConfigureServices(ApplyTestOverrides);
    }

    /// <summary>
    /// Every option in AddInfrastructure is validated with ValidateOnStart, so the host refuses to
    /// start unless each section is complete. These values satisfy the validators; the database and
    /// Redis point at the containers.
    /// </summary>
    private Dictionary<string, string?> TestSettings() => new()
    {
        ["ConnectionStrings:DefaultConnection"] = SqlConnectionString,

        ["JwtSettings:Secret"] = JwtSecret,
        ["JwtSettings:Issuer"] = JwtIssuer,
        ["JwtSettings:Audience"] = JwtAudience,
        ["JwtSettings:ExpiryMinutes"] = "15",
        ["JwtSettings:RefreshTokenExpiryDays"] = "7",

        ["RedisSettings:ConnectionString"] = _redisConnectionString,

        ["EmailSettings:Host"] = "localhost",
        ["EmailSettings:Port"] = "587",
        ["EmailSettings:UserName"] = "tests",
        ["EmailSettings:Password"] = "tests",
        ["EmailSettings:FromEmail"] = "tests@innoshop.local",
        ["EmailSettings:FromName"] = "InnoShop Tests",
        ["EmailSettings:ConfirmEmailUrl"] = "http://localhost/api/auth/confirm-email",
        ["EmailSettings:ResetPasswordUrl"] = "http://localhost/reset-password",

        // Left empty on purpose: tests create the admin explicitly so seeding is deterministic.
        ["AdminSeed:Email"] = "",
        ["AdminSeed:Password"] = "",
        ["AdminSeed:Name"] = "Test Administrator",

        ["RabbitMqSettings:Host"] = "localhost",
        ["RabbitMqSettings:Port"] = "5672",
        ["RabbitMqSettings:VirtualHost"] = "/",
        ["RabbitMqSettings:UserName"] = "tests",
        ["RabbitMqSettings:Password"] = "tests",

        ["Cors:AllowedOrigins:0"] = "http://localhost",
    };

    private void ApplyTestOverrides(IServiceCollection services)
    {
        services.RemoveAll<IEmailService>();
        services.AddSingleton<IEmailService>(Emails);

        services.RemoveAll<IUserEventPublisher>();
        services.AddSingleton<IUserEventPublisher>(Events);

        RemoveMassTransitHostedServices(services);
    }

    /// <summary>
    /// AddInfrastructure registers MassTransit against RabbitMQ. With no broker reachable the bus
    /// keeps retrying for the whole run, slowing every test down, so its hosted service is dropped.
    /// Events are still observable through <see cref="SpyEventPublisher"/>.
    /// </summary>
    private static void RemoveMassTransitHostedServices(IServiceCollection services)
    {
        var massTransitHostedServices = services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
            .Where(descriptor => descriptor.ImplementationType?.Assembly.GetName().Name
                ?.StartsWith("MassTransit", StringComparison.Ordinal) == true)
            .ToList();

        foreach (var descriptor in massTransitHostedServices)
            services.Remove(descriptor);
    }
}
