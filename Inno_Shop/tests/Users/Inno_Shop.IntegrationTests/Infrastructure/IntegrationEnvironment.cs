using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace Inno_Shop.IntegrationTests.Infrastructure;

/// <summary>
/// One SQL Server and one Redis container for the whole test assembly, started once and shared
/// by every test through a collection fixture. Starting them per test would add ~20 s each.
/// </summary>
public sealed class IntegrationEnvironment : IAsyncLifetime
{
    // SQL Server refuses to start unless the sa password satisfies its complexity policy.
    private const string SqlPassword = "InnoShop!Tests2026";

    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword(SqlPassword)
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public string SqlConnectionString { get; private set; } = string.Empty;

    public string RedisConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_sql.StartAsync(), _redis.StartAsync());

        SqlConnectionString = _sql.GetConnectionString();
        RedisConnectionString = _redis.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        await _sql.DisposeAsync();
        await _redis.DisposeAsync();
    }
}

[CollectionDefinition(IntegrationCollection.Name)]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationEnvironment>
{
    public const string Name = "integration";
}
