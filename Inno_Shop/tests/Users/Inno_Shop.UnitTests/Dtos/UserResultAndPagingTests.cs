using Inno_Shop.Users.Application.Dtos;
using Inno_Shop.Users.Domain;

namespace Inno_Shop.UnitTests.Dtos
{
    /// <summary>
    /// The small pieces of logic that live on the DTOs themselves.
    /// </summary>
    public class PagedResultTests
    {
        [Theory]
        [InlineData(42, 10, 5)]
        [InlineData(40, 10, 4)]
        [InlineData(41, 10, 5)]
        [InlineData(1, 10, 1)]
        [InlineData(0, 10, 0)]
        public void TotalPages_RoundsUp(int totalCount, int pageSize, int expected)
        {
            var page = new PagedResult<User>(Array.Empty<User>(), 1, pageSize, totalCount);

            page.TotalPages.Should().Be(expected);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void TotalPages_WithNonPositivePageSize_IsZero(int pageSize)
        {
            var page = new PagedResult<User>(Array.Empty<User>(), 1, pageSize, 100);

            page.TotalPages.Should().Be(0);
        }
    }

    public class UserResultTests
    {
        [Fact]
        public void Success_CarriesTheUserIdAndNoErrors()
        {
            var result = UserResult.Success("user-1");

            result.IsSuccess.Should().BeTrue();
            result.UserId.Should().Be("user-1");
            result.Errors.Should().BeEmpty();
            result.Type.Should().Be(ErrorType.None);
        }

        [Fact]
        public void Failure_WithSingleMessage_WrapsItInAList()
        {
            var result = UserResult.Failure("Something went wrong.", ErrorType.Conflict);

            result.IsSuccess.Should().BeFalse();
            result.UserId.Should().BeNull();
            result.Errors.Should().ContainSingle().Which.Should().Be("Something went wrong.");
            result.Type.Should().Be(ErrorType.Conflict);
        }

        [Fact]
        public void Failure_DefaultsToValidationErrorType()
        {
            var result = UserResult.Failure("Bad input.");

            result.Type.Should().Be(ErrorType.Validation);
        }

        [Fact]
        public void Failure_WithMultipleMessages_KeepsThemAll()
        {
            var errors = new List<string> { "First.", "Second." };

            var result = UserResult.Failure(errors, ErrorType.Validation);

            result.Errors.Should().BeEquivalentTo(errors);
        }
    }

    public class LoginResultTests
    {
        [Fact]
        public void IsSuccess_IsTrueOnlyForSuccessStatus()
        {
            new LoginResult(LoginStatus.Success, new TokenResponseDto("a", "r")).IsSuccess.Should().BeTrue();

            foreach (var status in new[]
                     {
                         LoginStatus.InvalidCredentials,
                         LoginStatus.EmailNotConfirmed,
                         LoginStatus.Deactivated,
                         LoginStatus.LockedOut
                     })
            {
                new LoginResult(status).IsSuccess.Should().BeFalse();
            }
        }
    }
}
