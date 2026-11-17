using Inno_Shop.Users.Application.Dtos;
using Inno_Shop.Users.Domain;

namespace Inno_Shop.UnitTests.Services
{
    /// <summary>
    /// Login and session flow of <see cref="Application.Services.UserService"/>.
    /// The ordering of the checks matters: credentials, then existence, then
    /// active state, then confirmation - tokens must only be issued when all pass.
    /// </summary>
    public class UserServiceAuthenticationTests : UserServiceTestBase
    {
        private const string Email = "john.doe@example.com";
        private const string Password = "Passw0rd!";
        private static readonly TokenResponseDto Tokens = new("access-token", "refresh-token");

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsTokens()
        {
            Repository.CheckCredentialsAsync(Email, Password).Returns(CredentialsCheck.Valid);
            Repository.GetByEmailAsync(Email).Returns(ActiveUser());
            TokenService.GenerateTokensAsync(Arg.Any<User>()).Returns(Tokens);

            var result = await Sut.LoginAsync(new LoginRequest(Email, Password));

            result.Status.Should().Be(LoginStatus.Success);
            result.IsSuccess.Should().BeTrue();
            result.Tokens.Should().Be(Tokens);
        }

        [Fact]
        public async Task LoginAsync_TrimsEmailBeforeCheckingCredentials()
        {
            Repository.CheckCredentialsAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(CredentialsCheck.Valid);
            Repository.GetByEmailAsync(Arg.Any<string>()).Returns(ActiveUser());
            TokenService.GenerateTokensAsync(Arg.Any<User>()).Returns(Tokens);

            await Sut.LoginAsync(new LoginRequest($"  {Email}  ", Password));

            await Repository.Received(1).CheckCredentialsAsync(Email, Password);
        }

        [Fact]
        public async Task LoginAsync_WithWrongPassword_ReturnsInvalidCredentialsAndNoTokens()
        {
            Repository.CheckCredentialsAsync(Email, Password).Returns(CredentialsCheck.Invalid);

            var result = await Sut.LoginAsync(new LoginRequest(Email, Password));

            result.Status.Should().Be(LoginStatus.InvalidCredentials);
            result.Tokens.Should().BeNull();
            await TokenService.DidNotReceiveWithAnyArgs().GenerateTokensAsync(default!);
            await Repository.DidNotReceiveWithAnyArgs().GetByEmailAsync(default!);
        }

        [Fact]
        public async Task LoginAsync_WhenAccountIsLockedOut_ReturnsLockedOut()
        {
            Repository.CheckCredentialsAsync(Email, Password).Returns(CredentialsCheck.LockedOut);

            var result = await Sut.LoginAsync(new LoginRequest(Email, Password));

            result.Status.Should().Be(LoginStatus.LockedOut);
            result.Tokens.Should().BeNull();
            await TokenService.DidNotReceiveWithAnyArgs().GenerateTokensAsync(default!);
        }

        [Fact]
        public async Task LoginAsync_WhenCredentialsPassButUserVanished_ReturnsInvalidCredentials()
        {
            Repository.CheckCredentialsAsync(Email, Password).Returns(CredentialsCheck.Valid);
            Repository.GetByEmailAsync(Email).Returns((User?)null);

            var result = await Sut.LoginAsync(new LoginRequest(Email, Password));

            result.Status.Should().Be(LoginStatus.InvalidCredentials);
            result.Tokens.Should().BeNull();
            await TokenService.DidNotReceiveWithAnyArgs().GenerateTokensAsync(default!);
        }

        [Fact]
        public async Task LoginAsync_WhenAccountIsDeactivated_ReturnsDeactivated()
        {
            var user = ActiveUser();
            user.IsActive = false;
            Repository.CheckCredentialsAsync(Email, Password).Returns(CredentialsCheck.Valid);
            Repository.GetByEmailAsync(Email).Returns(user);

            var result = await Sut.LoginAsync(new LoginRequest(Email, Password));

            result.Status.Should().Be(LoginStatus.Deactivated);
            result.Tokens.Should().BeNull();
            await TokenService.DidNotReceiveWithAnyArgs().GenerateTokensAsync(default!);
        }

        [Fact]
        public async Task LoginAsync_WhenEmailIsNotConfirmed_ReturnsEmailNotConfirmed()
        {
            Repository.CheckCredentialsAsync(Email, Password).Returns(CredentialsCheck.Valid);
            Repository.GetByEmailAsync(Email).Returns(UnconfirmedUser(email: Email));

            var result = await Sut.LoginAsync(new LoginRequest(Email, Password));

            result.Status.Should().Be(LoginStatus.EmailNotConfirmed);
            result.Tokens.Should().BeNull();
            await TokenService.DidNotReceiveWithAnyArgs().GenerateTokensAsync(default!);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithValidToken_ReturnsNewTokens()
        {
            TokenService.RefreshTokensAsync("refresh-token").Returns(Tokens);

            var result = await Sut.RefreshTokenAsync("refresh-token");

            result.Should().Be(Tokens);
        }

        [Fact]
        public async Task RefreshTokenAsync_WhenTokenIsRejected_ReturnsNullInsteadOfThrowing()
        {
            TokenService.RefreshTokensAsync("stale-token")
                .Returns<Task<TokenResponseDto>>(_ => throw new UnauthorizedAccessException("expired"));

            var result = await Sut.RefreshTokenAsync("stale-token");

            result.Should().BeNull();
        }

        [Fact]
        public async Task RefreshTokenAsync_DoesNotSwallowUnexpectedFailures()
        {
            TokenService.RefreshTokensAsync("refresh-token")
                .Returns<Task<TokenResponseDto>>(_ => throw new InvalidOperationException("redis is down"));

            var act = () => Sut.RefreshTokenAsync("refresh-token");

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task LogoutAsync_RevokesTheSuppliedRefreshToken()
        {
            await Sut.LogoutAsync("refresh-token");

            await TokenService.Received(1).RevokeRefreshTokenAsync("refresh-token");
        }
    }
}
