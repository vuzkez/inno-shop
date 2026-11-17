using Inno_Shop.Users.Application.Dtos;
using Inno_Shop.Users.Domain;

namespace Inno_Shop.UnitTests.Services
{
    /// <summary>
    /// Profile reads/updates and the administrative lifecycle
    /// (create, deactivate, activate, delete) of
    /// <see cref="Application.Services.UserService"/>.
    /// </summary>
    public class UserServiceManagementTests : UserServiceTestBase
    {
        [Fact]
        public async Task GetProfileAsync_WhenUserExists_MapsDomainUserToDto()
        {
            Repository.GetByIdAsync("user-1")
                .Returns(ActiveUser(role: Role.Admin));

            var profile = await Sut.GetProfileAsync("user-1");

            profile.Should().NotBeNull();
            profile!.Id.Should().Be("user-1");
            profile.Email.Should().Be("john.doe@example.com");
            profile.Name.Should().Be("John Doe");
            profile.Role.Should().Be("Admin");
            profile.IsActive.Should().BeTrue();
            profile.IsEmailConfirmed.Should().BeTrue();
        }

        [Fact]
        public async Task GetProfileAsync_WhenUserDoesNotExist_ReturnsNull()
        {
            Repository.GetByIdAsync("missing").Returns((User?)null);

            var profile = await Sut.GetProfileAsync("missing");

            profile.Should().BeNull();
        }

        [Fact]
        public async Task GetUsersAsync_MapsEveryItemAndKeepsPagingMetadata()
        {
            Repository.GetUsersAsync(Arg.Any<UserListQuery>())
                .Returns(new PagedResult<User>(
                    new[] { ActiveUser("user-1"), ActiveUser("user-2", "jane@example.com", "Jane") },
                    Page: 2,
                    PageSize: 10,
                    TotalCount: 42));

            var page = await Sut.GetUsersAsync(new UserListQuery(Page: 2, PageSize: 10));

            page.Items.Should().HaveCount(2);
            page.Items.Select(i => i.Id).Should().Equal("user-1", "user-2");
            page.Page.Should().Be(2);
            page.PageSize.Should().Be(10);
            page.TotalCount.Should().Be(42);
            page.TotalPages.Should().Be(5);
        }

        [Fact]
        public async Task UpdateProfileAsync_WhenUserDoesNotExist_ReturnsNotFound()
        {
            Repository.GetByIdAsync("missing").Returns((User?)null);

            var result = await Sut.UpdateProfileAsync("missing", new UpdateProfileRequest("New Name", null));

            result.IsSuccess.Should().BeFalse();
            result.Type.Should().Be(ErrorType.NotFound);
            await Repository.DidNotReceiveWithAnyArgs().UpdateUserAsync(default!);
            await EmailService.DidNotReceiveWithAnyArgs().SendEmailConfirmationAsync(default!, default!, default!);
        }

        [Fact]
        public async Task UpdateProfileAsync_WithOnlyNameChanged_DoesNotSendConfirmation()
        {
            var user = ActiveUser();
            Repository.GetByIdAsync(user.Id).Returns(user);
            Repository.UpdateUserAsync(Arg.Any<User>()).Returns(UserResult.Success(user.Id));

            var result = await Sut.UpdateProfileAsync(user.Id, new UpdateProfileRequest("  Johnny  ", null));

            result.IsSuccess.Should().BeTrue();
            user.Name.Should().Be("Johnny");
            await Repository.Received(1).UpdateUserAsync(user);
            await EmailService.DidNotReceiveWithAnyArgs().SendEmailConfirmationAsync(default!, default!, default!);
        }

        [Fact]
        public async Task UpdateProfileAsync_WhenEmailChanges_AssignsItAndSendsConfirmationToNewAddress()
        {
            var user = ActiveUser();
            Repository.GetByIdAsync(user.Id).Returns(user);
            Repository.UpdateUserAsync(Arg.Any<User>()).Returns(UserResult.Success(user.Id));
            Repository.GenerateEmailConfirmationTokenAsync(user.Id).Returns("confirm-token");

            var result = await Sut.UpdateProfileAsync(user.Id, new UpdateProfileRequest(null, " new@example.com "));

            result.IsSuccess.Should().BeTrue();
            user.Email.Should().Be("new@example.com");
            await EmailService.Received(1).SendEmailConfirmationAsync("new@example.com", user.Id, "confirm-token");
        }

        [Fact]
        public async Task UpdateProfileAsync_WhenEmailDiffersOnlyByCaseOrSpacing_IsNotTreatedAsAChange()
        {
            var user = ActiveUser(email: "John.Doe@Example.com");
            Repository.GetByIdAsync(user.Id).Returns(user);
            Repository.UpdateUserAsync(Arg.Any<User>()).Returns(UserResult.Success(user.Id));

            await Sut.UpdateProfileAsync(user.Id, new UpdateProfileRequest(null, "  john.doe@example.com  "));

            user.Email.Should().Be("John.Doe@Example.com");
            await EmailService.DidNotReceiveWithAnyArgs().SendEmailConfirmationAsync(default!, default!, default!);
        }

        [Fact]
        public async Task UpdateProfileAsync_WithWhitespaceOnlyEmail_IsIgnored()
        {
            var user = ActiveUser();
            Repository.GetByIdAsync(user.Id).Returns(user);
            Repository.UpdateUserAsync(Arg.Any<User>()).Returns(UserResult.Success(user.Id));

            await Sut.UpdateProfileAsync(user.Id, new UpdateProfileRequest(null, "   "));

            user.Email.Should().Be("john.doe@example.com");
            await EmailService.DidNotReceiveWithAnyArgs().SendEmailConfirmationAsync(default!, default!, default!);
        }

        [Fact]
        public async Task UpdateProfileAsync_WhenEmailChangeIsRejectedByRepository_DoesNotSendConfirmation()
        {
            var user = ActiveUser();
            Repository.GetByIdAsync(user.Id).Returns(user);
            Repository.UpdateUserAsync(Arg.Any<User>())
                .Returns(UserResult.Failure("Email is already taken.", ErrorType.Conflict));

            var result = await Sut.UpdateProfileAsync(user.Id, new UpdateProfileRequest(null, "taken@example.com"));

            result.IsSuccess.Should().BeFalse();
            result.Type.Should().Be(ErrorType.Conflict);
            await EmailService.DidNotReceiveWithAnyArgs().SendEmailConfirmationAsync(default!, default!, default!);
        }

        [Fact]
        public async Task CreateUserAsync_ParsesRoleIgnoringCase_AndMarksEmailConfirmed()
        {
            Repository
                .CreateUserAsync("Jane Doe", "jane@example.com", "Passw0rd!", Role.Admin, true)
                .Returns(UserResult.Success("user-9"));

            var result = await Sut.CreateUserAsync(
                new CreateUserRequest("  Jane Doe ", " jane@example.com ", "Passw0rd!", "aDmIn"));

            result.IsSuccess.Should().BeTrue();
            result.UserId.Should().Be("user-9");
            await Repository.Received(1).CreateUserAsync(
                "Jane Doe", "jane@example.com", "Passw0rd!", Role.Admin, true);
        }

        [Fact]
        public async Task CreateUserAsync_WithUnknownRole_Throws()
        {
            var act = () => Sut.CreateUserAsync(
                new CreateUserRequest("Jane Doe", "jane@example.com", "Passw0rd!", "SuperUser"));

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task DeactivateAsync_TerminatesSessionsAndHidesProducts()
        {
            Repository.DeactivateUserAsync("user-1").Returns(true);

            var result = await Sut.DeactivateAsync("user-1");

            result.IsSuccess.Should().BeTrue();
            result.UserId.Should().Be("user-1");
            await TokenService.Received(1).RevokeAllUserTokensAsync("user-1");
            await EventPublisher.Received(1).DeactivateUserProductsAsync("user-1", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeactivateAsync_WhenUserDoesNotExist_ReturnsNotFoundAndPublishesNothing()
        {
            Repository.DeactivateUserAsync("missing").Returns(false);

            var result = await Sut.DeactivateAsync("missing");

            result.IsSuccess.Should().BeFalse();
            result.Type.Should().Be(ErrorType.NotFound);
            await TokenService.DidNotReceiveWithAnyArgs().RevokeAllUserTokensAsync(default!);
            await EventPublisher.DidNotReceiveWithAnyArgs()
                .DeactivateUserProductsAsync(default!, default);
        }

        [Fact]
        public async Task ActivateAsync_RestoresProductsButKeepsSessionsTerminated()
        {
            Repository.ActivateUserAsync("user-1").Returns(true);

            var result = await Sut.ActivateAsync("user-1");

            result.IsSuccess.Should().BeTrue();
            await EventPublisher.Received(1).ActivateUserProductsAsync("user-1", Arg.Any<CancellationToken>());
            await TokenService.DidNotReceiveWithAnyArgs().RevokeAllUserTokensAsync(default!);
        }

        [Fact]
        public async Task ActivateAsync_WhenUserDoesNotExist_ReturnsNotFoundAndPublishesNothing()
        {
            Repository.ActivateUserAsync("missing").Returns(false);

            var result = await Sut.ActivateAsync("missing");

            result.IsSuccess.Should().BeFalse();
            result.Type.Should().Be(ErrorType.NotFound);
            await EventPublisher.DidNotReceiveWithAnyArgs().ActivateUserProductsAsync(default!, default);
        }

        [Fact]
        public async Task DeleteAsync_RemovesProductsAndSessions()
        {
            Repository.DeleteUserAsync("user-2").Returns(true);

            var result = await Sut.DeleteAsync("user-2", actorId: "admin-1");

            result.IsSuccess.Should().BeTrue();
            await EventPublisher.Received(1).DeleteUserProductsAsync("user-2", Arg.Any<CancellationToken>());
            await TokenService.Received(1).RevokeAllUserTokensAsync("user-2");
        }

        [Fact]
        public async Task DeleteAsync_WhenActorDeletesThemselves_IsForbiddenAndTouchesNothing()
        {
            var result = await Sut.DeleteAsync("user-1", actorId: "user-1");

            result.IsSuccess.Should().BeFalse();
            result.Type.Should().Be(ErrorType.Forbidden);
            result.Errors.Should().ContainSingle().Which.Should().Be("You cannot delete your own account.");
            await Repository.DidNotReceiveWithAnyArgs().DeleteUserAsync(default!);
            await EventPublisher.DidNotReceiveWithAnyArgs().DeleteUserProductsAsync(default!, default);
            await TokenService.DidNotReceiveWithAnyArgs().RevokeAllUserTokensAsync(default!);
        }

        [Fact]
        public async Task DeleteAsync_WhenUserDoesNotExist_ReturnsNotFound()
        {
            Repository.DeleteUserAsync("missing").Returns(false);

            var result = await Sut.DeleteAsync("missing", actorId: "admin-1");

            result.IsSuccess.Should().BeFalse();
            result.Type.Should().Be(ErrorType.NotFound);
            await EventPublisher.DidNotReceiveWithAnyArgs().DeleteUserProductsAsync(default!, default);
            await TokenService.DidNotReceiveWithAnyArgs().RevokeAllUserTokensAsync(default!);
        }
    }
}
