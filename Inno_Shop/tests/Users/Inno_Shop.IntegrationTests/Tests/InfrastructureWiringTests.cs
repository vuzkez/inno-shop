using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Inno_Shop.IntegrationTests.Infrastructure;
using Inno_Shop.Users.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Inno_Shop.IntegrationTests.Tests;

/// <summary>
/// Guards the test harness itself: these tests fail loudly if the application ever stops talking to
/// a real SQL Server or a real Redis (for example if someone swaps the DbContext back to InMemory
/// or fakes the token service again). Without them the suite would stay green while silently
/// testing nothing but mocks.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class InfrastructureWiringTests(IntegrationEnvironment environment) : ApiTestBase(environment)
{
    private const string Email = "wiring.user@example.com";

    [Fact]
    public async Task Database_IsARealSqlServerInstance()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

        db.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.SqlServer");

        var canConnect = await db.Database.CanConnectAsync();
        canConnect.Should().BeTrue();
    }

    [Fact]
    public async Task Database_IsCreatedByEfMigrations()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

        var applied = await db.Database.GetAppliedMigrationsAsync();

        applied.Should().NotBeEmpty("the schema must come from the real EF migrations, not EnsureCreated");
    }

    /// <summary>
    /// Proves the production <c>TokenService</c> is in play and that it writes to the Redis
    /// container: the refresh token is stored there under its SHA-256 hash.
    /// </summary>
    [Fact]
    public async Task Login_StoresTheRefreshTokenHashInRedis()
    {
        var registration = await RegisterConfirmedUserAsync(Email);
        var tokens = await LoginAsync(Email);

        var redis = Factory.Services.GetRequiredService<IConnectionMultiplexer>();
        var cache = redis.GetDatabase();

        var expectedHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(tokens.RefreshToken)));

        (await cache.KeyExistsAsync($"refresh:{expectedHash}")).Should().BeTrue(
            because: "the real TokenService stores the refresh token hash in Redis");

        (await cache.SetContainsAsync($"user-refresh:{registration.UserId}", expectedHash)).Should().BeTrue(
            because: "tokens are indexed per user so they can all be revoked at once");
    }

    /// <summary>
    /// The refresh token is single use in production (Redis GETDEL). If the fake token service ever
    /// came back, this would still pass - but combined with the hash assertion above it pins the
    /// real implementation.
    /// </summary>
    [Fact]
    public async Task Refresh_ConsumesTheStoredToken()
    {
        await RegisterConfirmedUserAsync(Email);
        var tokens = await LoginAsync(Email);

        var expectedHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(tokens.RefreshToken)));

        var refresh = await Client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = tokens.RefreshToken });
        refresh.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var redis = Factory.Services.GetRequiredService<IConnectionMultiplexer>();
        (await redis.GetDatabase().KeyExistsAsync($"refresh:{expectedHash}")).Should().BeFalse(
            because: "the consumed token must be deleted from Redis");
    }
}
