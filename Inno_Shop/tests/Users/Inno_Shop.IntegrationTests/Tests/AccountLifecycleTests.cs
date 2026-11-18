using System.Net;
using System.Net.Http.Json;
using Inno_Shop.IntegrationTests.Infrastructure;

namespace Inno_Shop.IntegrationTests.Tests;

/// <summary>
/// Administrative lifecycle: deactivation must block login and publish the event the Products
/// service will consume (requirement 2.1 of the spec), activation must restore access, and a
/// user must not be able to delete their own account.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class AccountLifecycleTests(IntegrationEnvironment environment) : ApiTestBase(environment)
{
    private const string Email = "lifecycle.user@example.com";

    [Fact]
    public async Task Deactivate_ByAdmin_BlocksLogin_AndPublishesEvent()
    {
        var registration = await RegisterConfirmedUserAsync(Email);
        var (_, adminToken) = await CreateAdminAsync();

        UseToken(adminToken);
        var deactivate = await Client.PostAsync($"/api/users/{registration.UserId}/deactivate", content: null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var login = await TryLoginAsync(Email);
        login.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        Factory.Events.DeactivatedUsers.Should().Contain(registration.UserId,
            because: "deactivation must notify the Products service to hide the user's products");
    }

    [Fact]
    public async Task Activate_AfterDeactivation_RestoresLogin_AndPublishesEvent()
    {
        var registration = await RegisterConfirmedUserAsync(Email);
        var (_, adminToken) = await CreateAdminAsync();

        UseToken(adminToken);
        await Client.PostAsync($"/api/users/{registration.UserId}/deactivate", content: null);
        var activate = await Client.PostAsync($"/api/users/{registration.UserId}/activate", content: null);
        activate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var login = await TryLoginAsync(Email);
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        Factory.Events.ActivatedUsers.Should().Contain(registration.UserId);
    }

    [Fact]
    public async Task Deactivate_RevokesTheUsersSessions()
    {
        var registration = await RegisterConfirmedUserAsync(Email);
        var userTokens = await LoginAsync(Email);
        var (_, adminToken) = await CreateAdminAsync();

        UseToken(adminToken);
        var deactivate = await Client.PostAsync($"/api/users/{registration.UserId}/deactivate", content: null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refresh = await Client.PostAsJsonAsync(
            "/api/auth/refresh", new { refreshToken = userTokens.RefreshToken });

        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: "deactivation terminates every active session");
    }

    [Fact]
    public async Task UpdateMe_ChangingEmail_SendsConfirmationToTheNewAddress()
    {
        await RegisterConfirmedUserAsync(Email);
        var tokens = await LoginAsync(Email);

        UseToken(tokens.AccessToken);
        var response = await Client.PutAsJsonAsync("/api/users/me", new { email = "changed@example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var confirmation = LatestConfirmationFor("changed@example.com");
        confirmation.Token.Should().NotBeNullOrWhiteSpace();
        confirmation.Email.Should().Be("changed@example.com");
    }

    [Fact]
    public async Task Delete_Self_IsForbidden()
    {
        var (adminId, adminToken) = await CreateAdminAsync();

        UseToken(adminToken);
        var response = await Client.DeleteAsync($"/api/users/{adminId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        Factory.Events.DeletedUsers.Should().NotContain(adminId);
    }

    [Fact]
    public async Task Delete_ByAdmin_RemovesTheUser_AndPublishesEvent()
    {
        var registration = await RegisterConfirmedUserAsync(Email);
        var (_, adminToken) = await CreateAdminAsync();

        UseToken(adminToken);
        var delete = await Client.DeleteAsync($"/api/users/{registration.UserId}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var login = await TryLoginAsync(Email);
        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: "the account no longer exists, so credentials are simply invalid");

        Factory.Events.DeletedUsers.Should().Contain(registration.UserId);
    }

    [Fact]
    public async Task Deactivate_UnknownUser_ReturnsNotFound()
    {
        var (_, adminToken) = await CreateAdminAsync();

        UseToken(adminToken);
        var response = await Client.PostAsync("/api/users/no-such-user/deactivate", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ForgotPassword_ForUnknownEmail_StaysSilentAndReturns202()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/forgot-password", new { email = "ghost@example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        Factory.Emails.PasswordResets.Should().BeEmpty(
            because: "the endpoint must not reveal whether the address exists");
    }

    [Fact]
    public async Task ForgotPassword_ForKnownEmail_SendsResetLink()
    {
        await RegisterConfirmedUserAsync(Email);

        var response = await Client.PostAsJsonAsync("/api/auth/forgot-password", new { email = Email });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        Factory.Emails.PasswordResets.Should().ContainSingle(r => r.Email == Email);
    }
}
