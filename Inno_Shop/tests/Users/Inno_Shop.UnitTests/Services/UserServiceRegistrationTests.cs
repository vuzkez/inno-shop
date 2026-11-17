using Inno_Shop.Users.Application.Dtos;
using Inno_Shop.Users.Domain;

namespace Inno_Shop.UnitTests.Services
{
    /// <summary>
    /// Registration and e-mail confirmation flow of <see cref="Application.Services.UserService"/>.
    /// </summary>
    public class UserServiceRegistrationTests : UserServiceTestBase
    {
        [Fact]
        public async Task RegisterAsync_WithValidRequest_CreatesCustomerAndSendsConfirmation()
        {
            Repository
                .CreateUserAsync("John Doe", "john.doe@example.com", "Passw0rd!", Role.Customer, false)
                .Returns(UserResult.Success("user-1"));
            Repository.GenerateEmailConfirmationTokenAsync("user-1").Returns("confirm-token");

            var result = await Sut.RegisterAsync(new RegisterRequest("John Doe", "john.doe@example.com", "Passw0rd!"));

            result.IsSuccess.Should().BeTrue();
            result.UserId.Should().Be("user-1");
            await Repository.Received(1).CreateUserAsync(
                "John Doe", "john.doe@example.com", "Passw0rd!", Role.Customer, false);
            await EmailService.Received(1).SendEmailConfirmationAsync(
                "john.doe@example.com", "user-1", "confirm-token");
        }

        [Fact]
        public async Task RegisterAsync_TrimsNameAndEmail_ButLeavesPasswordUntouched()
        {
            Repository
                .CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Role>(), Arg.Any<bool>())
                .Returns(UserResult.Success("user-1"));

            await Sut.RegisterAsync(new RegisterRequest("  John Doe  ", "  john.doe@example.com  ", " Passw0rd! "));

            await Repository.Received(1).CreateUserAsync(
                "John Doe", "john.doe@example.com", " Passw0rd! ", Role.Customer, false);
        }

        [Fact]
        public async Task RegisterAsync_WhenRepositoryFails_ReturnsFailureAndSendsNoEmail()
        {
            var failure = UserResult.Failure("Email is already taken.", ErrorType.Conflict);
            Repository
                .CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Role>(), Arg.Any<bool>())
                .Returns(failure);

            var result = await Sut.RegisterAsync(new RegisterRequest("John Doe", "taken@example.com", "Passw0rd!"));

            result.Should().BeSameAs(failure);
            result.IsSuccess.Should().BeFalse();
            result.Type.Should().Be(ErrorType.Conflict);
            await EmailService.DidNotReceiveWithAnyArgs().SendEmailConfirmationAsync(default!, default!, default!);
        }

        [Fact]
        public async Task RegisterAsync_WhenConfirmationTokenIsEmpty_SendsNoEmail()
        {
            Repository
                .CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Role>(), Arg.Any<bool>())
                .Returns(UserResult.Success("user-1"));
            Repository.GenerateEmailConfirmationTokenAsync("user-1").Returns(string.Empty);

            var result = await Sut.RegisterAsync(new RegisterRequest("John Doe", "john.doe@example.com", "Passw0rd!"));

            result.IsSuccess.Should().BeTrue();
            await EmailService.DidNotReceiveWithAnyArgs().SendEmailConfirmationAsync(default!, default!, default!);
        }

        [Fact]
        public async Task ConfirmEmailAsync_OnSuccess_ReturnsRepositoryResult()
        {
            var success = UserResult.Success("user-1");
            Repository.ConfirmEmailAsync("user-1", "token").Returns(success);

            var result = await Sut.ConfirmEmailAsync(new ConfirmEmailRequest("user-1", "token"));

            result.Should().BeSameAs(success);
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task ConfirmEmailAsync_OnFailure_ReplacesDetailsWithGenericMessage()
        {
            Repository.ConfirmEmailAsync("user-1", "bad-token")
                .Returns(UserResult.Failure("User not found.", ErrorType.NotFound));

            var result = await Sut.ConfirmEmailAsync(new ConfirmEmailRequest("user-1", "bad-token"));

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle().Which.Should().Be("Invalid or expired token.");
            result.Errors.Should().NotContain("User not found.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("unknown@example.com")]
        public async Task ResendConfirmationAsync_WhenUserIsMissing_DoesNothing(string? email)
        {
            Repository.GetByEmailAsync(Arg.Any<string>()).Returns((User?)null);

            await Sut.ResendConfirmationAsync(email ?? "unknown@example.com");

            await EmailService.DidNotReceiveWithAnyArgs().SendEmailConfirmationAsync(default!, default!, default!);
        }

        [Fact]
        public async Task ResendConfirmationAsync_WhenUserIsDeactivated_DoesNothing()
        {
            var user = ActiveUser();
            user.IsActive = false;
            Repository.GetByEmailAsync(user.Email).Returns(user);

            await Sut.ResendConfirmationAsync(user.Email);

            await EmailService.DidNotReceiveWithAnyArgs().SendEmailConfirmationAsync(default!, default!, default!);
        }

        [Fact]
        public async Task ResendConfirmationAsync_WhenEmailAlreadyConfirmed_DoesNothing()
        {
            Repository.GetByEmailAsync("john.doe@example.com").Returns(ActiveUser(isEmailConfirmed: true));

            await Sut.ResendConfirmationAsync("john.doe@example.com");

            await EmailService.DidNotReceiveWithAnyArgs().SendEmailConfirmationAsync(default!, default!, default!);
        }

        [Fact]
        public async Task ResendConfirmationAsync_ForActiveUnconfirmedUser_SendsNewLink()
        {
            Repository.GetByEmailAsync("john.doe@example.com").Returns(UnconfirmedUser());
            Repository.GenerateEmailConfirmationTokenAsync("user-1").Returns("fresh-token");

            await Sut.ResendConfirmationAsync("  john.doe@example.com  ");

            await EmailService.Received(1).SendEmailConfirmationAsync(
                "john.doe@example.com", "user-1", "fresh-token");
        }

        [Fact]
        public async Task SendForgotPasswordEmailAsync_ForActiveUser_SendsResetLink()
        {
            Repository.GetByEmailAsync("john.doe@example.com").Returns(ActiveUser());
            Repository.GeneratePasswordResetTokenAsync("john.doe@example.com").Returns("reset-token");

            await Sut.SendForgotPasswordEmailAsync(" john.doe@example.com ");

            await EmailService.Received(1).SendPasswordResetAsync("john.doe@example.com", "reset-token");
        }

        [Fact]
        public async Task SendForgotPasswordEmailAsync_WhenUserDoesNotExist_StaysSilent()
        {
            Repository.GetByEmailAsync(Arg.Any<string>()).Returns((User?)null);

            await Sut.SendForgotPasswordEmailAsync("ghost@example.com");

            await EmailService.DidNotReceiveWithAnyArgs().SendPasswordResetAsync(default!, default!);
        }

        [Fact]
        public async Task SendForgotPasswordEmailAsync_WhenUserIsDeactivated_StaysSilent()
        {
            var user = ActiveUser();
            user.IsActive = false;
            Repository.GetByEmailAsync(user.Email).Returns(user);

            await Sut.SendForgotPasswordEmailAsync(user.Email);

            await EmailService.DidNotReceiveWithAnyArgs().SendPasswordResetAsync(default!, default!);
        }

        [Fact]
        public async Task ResetPasswordAsync_OnSuccess_RevokesEverySession()
        {
            Repository
                .ResetPasswordAsync("john.doe@example.com", "reset-token", "NewPassw0rd!")
                .Returns(UserResult.Success("user-1"));

            var result = await Sut.ResetPasswordAsync(
                new ResetPasswordRequest(" john.doe@example.com ", "reset-token", "NewPassw0rd!"));

            result.IsSuccess.Should().BeTrue();
            await TokenService.Received(1).RevokeAllUserTokensAsync("user-1");
        }

        [Fact]
        public async Task ResetPasswordAsync_WhenUserNotFound_MasksItAsInvalidToken()
        {
            Repository
                .ResetPasswordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(UserResult.Failure("User not found.", ErrorType.NotFound));

            var result = await Sut.ResetPasswordAsync(
                new ResetPasswordRequest("ghost@example.com", "reset-token", "NewPassw0rd!"));

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle().Which.Should().Be("Invalid or expired token.");
            await TokenService.DidNotReceiveWithAnyArgs().RevokeAllUserTokensAsync(default!);
        }

        [Fact]
        public async Task ResetPasswordAsync_WhenPasswordIsRejected_KeepsValidationErrors()
        {
            Repository
                .ResetPasswordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(UserResult.Failure("Password too weak.", ErrorType.Validation));

            var result = await Sut.ResetPasswordAsync(
                new ResetPasswordRequest("john.doe@example.com", "reset-token", "weak"));

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle().Which.Should().Be("Password too weak.");
            await TokenService.DidNotReceiveWithAnyArgs().RevokeAllUserTokensAsync(default!);
        }
    }
}
