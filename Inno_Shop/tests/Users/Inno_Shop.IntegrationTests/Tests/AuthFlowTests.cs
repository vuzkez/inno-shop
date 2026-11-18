using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Inno_Shop.IntegrationTests.Infrastructure;

namespace Inno_Shop.IntegrationTests.Tests;

/// <summary>
/// The end-to-end registration/authentication journey. This is the class of bug that unit
/// tests cannot catch: the pieces are individually correct but wired together wrongly
/// (claim names, DI lifetimes, middleware order, Identity configuration).
/// </summary>
[Collection(IntegrationCollection.Name)]
public class AuthFlowTests(IntegrationEnvironment environment) : ApiTestBase(environment)
{
    private const string Email = "john.doe@example.com";
    private const string Password = "Passw0rd!";

    [Fact]
    public async Task Register_ReturnsCreated_AndQueuesConfirmationEmail()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/register", new { name = "John Doe", email = Email, password = Password });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = (await response.Content.ReadFromJsonAsync<RegisterResponse>())!;
        body.UserId.Should().NotBeNullOrWhiteSpace();

        var confirmation = LatestConfirmationFor(Email);
        confirmation.UserId.Should().Be(body.UserId);
        confirmation.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_BeforeEmailIsConfirmed_IsForbidden()
    {
        await RegisterAsync(Email);

        var response = await TryLoginAsync(Email);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("title").GetString().Should().Be("Email is not confirmed");
    }

    [Fact]
    public async Task Register_ThenConfirm_ThenLogin_IssuesTokensAndProfileIsReadable()
    {
        var registration = await RegisterAsync(Email);

        await ConfirmEmailAsync(LatestConfirmationFor(Email));

        var tokens = await LoginAsync(Email);
        tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var profile = await GetMeAsync(tokens.AccessToken);
        profile.Id.Should().Be(registration.UserId);
        profile.Email.Should().Be(Email);
        profile.Name.Should().Be("John Doe");
        profile.Role.Should().Be("Customer");
        profile.IsActive.Should().BeTrue();
        profile.IsEmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmEmail_WithBadToken_IsRejected()
    {
        await RegisterAsync(Email);
        var confirmation = LatestConfirmationFor(Email);

        var response = await Client.GetAsync(
            $"/api/auth/confirm-email?userId={Uri.EscapeDataString(confirmation.UserId)}&token=not-a-real-token");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("detail").GetString().Should().Be("Invalid or expired token.");
    }

    [Fact]
    public async Task Refresh_RotatesTheToken_AndTheOldTokenStopsWorking()
    {
        await RegisterConfirmedUserAsync(Email);
        var tokens = await LoginAsync(Email);

        var refreshResponse = await Client.PostAsJsonAsync(
            "/api/auth/refresh", new { refreshToken = tokens.RefreshToken });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = (await refreshResponse.Content.ReadFromJsonAsync<TokensResponse>())!;
        refreshed.RefreshToken.Should().NotBe(tokens.RefreshToken, because: "refresh tokens are single use");

        // Replaying the consumed token must fail.
        var replay = await Client.PostAsJsonAsync(
            "/api/auth/refresh", new { refreshToken = tokens.RefreshToken });

        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshToken()
    {
        await RegisterConfirmedUserAsync(Email);
        var tokens = await LoginAsync(Email);

        var logout = await Client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = tokens.RefreshToken });
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refreshAfterLogout = await Client.PostAsJsonAsync(
            "/api/auth/refresh", new { refreshToken = tokens.RefreshToken });

        refreshAfterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400WithFieldErrors()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/register", new { name = "John Doe", email = Email, password = "weak" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("title").GetString()
            .Should().Be("One or more validation errors occurred.");

        problem.RootElement.GetProperty("errors")
            .TryGetProperty("Password", out var passwordErrors)
            .Should().BeTrue(because: "the validation filter keys errors by property name");

        passwordErrors.EnumerateArray().Select(e => e.GetString())
            .Should().Contain(m => m!.Contains("must be at least 8 characters"));
    }

    [Fact]
    public async Task Register_WithMalformedEmail_Returns400()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/register", new { name = "John Doe", email = "not-an-email", password = Password });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("errors").TryGetProperty("Email", out _)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Register_WithAnEmailThatIsAlreadyTaken_Returns409()
    {
        await RegisterAsync(Email);

        var response = await Client.PostAsJsonAsync(
            "/api/auth/register", new { name = "Someone Else", email = Email, password = Password });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("title").GetString().Should().Be("Conflict");
    }
}
