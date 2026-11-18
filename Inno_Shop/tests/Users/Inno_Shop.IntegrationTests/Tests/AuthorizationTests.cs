using System.Net;
using System.Net.Http.Json;
using Inno_Shop.IntegrationTests.Infrastructure;

namespace Inno_Shop.IntegrationTests.Tests;

/// <summary>
/// Verifies that the JWT produced by <c>JwtProvider</c> is actually accepted by the validation
/// parameters configured in Program.cs ("role" claim type, MapInboundClaims=false, sub claim),
/// and that role-based authorization returns the right status codes.
/// This is exactly the wiring that silently breaks: token issued fine, every call returns 403.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class AuthorizationTests(IntegrationEnvironment environment) : ApiTestBase(environment)
{
    private const string Email = "jane.doe@example.com";

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithGarbageToken_Returns401()
    {
        UseToken("this.is.not-a-valid-jwt");

        var response = await Client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminOnlyEndpoint_AsCustomer_Returns403()
    {
        await RegisterConfirmedUserAsync(Email);
        var tokens = await LoginAsync(Email);

        UseToken(tokens.AccessToken);
        var response = await Client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminOnlyEndpoint_AsAdmin_Returns200()
    {
        var (_, adminToken) = await CreateAdminAsync();
        await RegisterConfirmedUserAsync(Email);

        UseToken(adminToken);
        var response = await Client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: "logs:\n{0}", DumpLogs());

        var page = (await response.Content.ReadFromJsonAsync<PagedUsersResponse>())!;
        page.Items.Should().Contain(u => u.Email == Email);
        page.Items.Should().Contain(u => u.Role == "Admin");
    }

    /// <summary>
    /// Exercises the real <c>UserRepository.GetUsersAsync</c> LINQ query (joins over
    /// UserRoles/Roles). Unit tests mock this method away, so a broken query would only
    /// surface here.
    /// </summary>
    [Fact]
    public async Task AdminOnlyEndpoint_AppliesSearchFilter()
    {
        var (_, adminToken) = await CreateAdminAsync();
        await RegisterConfirmedUserAsync("alice.smith@example.com", name: "Alice Smith");
        await RegisterConfirmedUserAsync("bob.jones@example.com", name: "Bob Jones");

        UseToken(adminToken);
        var response = await Client.GetAsync("/api/users?search=Alice");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = (await response.Content.ReadFromJsonAsync<PagedUsersResponse>())!;
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle().Which.Email.Should().Be("alice.smith@example.com");
    }

    [Fact]
    public async Task GetUserById_AsAdmin_ReturnsNotFoundForUnknownId()
    {
        var (_, adminToken) = await CreateAdminAsync();

        UseToken(adminToken);
        var response = await Client.GetAsync("/api/users/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
