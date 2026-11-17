using Inno_Shop.Users.Application.Interfaces;
using Inno_Shop.Users.Application.Services;
using Inno_Shop.Users.Domain;

namespace Inno_Shop.UnitTests.Services
{
    /// <summary>
    /// Shared fixture for <see cref="UserService"/> tests.
    /// Every collaborator is a NSubstitute double, so these tests cover only the
    /// business rules that live inside the service: branching, trimming, masking of
    /// errors, ordering of side effects. No database, no Redis, no SMTP.
    /// </summary>
    public abstract class UserServiceTestBase
    {
        protected readonly IUserRepository Repository = Substitute.For<IUserRepository>();
        protected readonly IEmailService EmailService = Substitute.For<IEmailService>();
        protected readonly ITokenService TokenService = Substitute.For<ITokenService>();
        protected readonly IUserEventPublisher EventPublisher = Substitute.For<IUserEventPublisher>();

        protected readonly UserService Sut;

        protected UserServiceTestBase()
        {
            Sut = new UserService(Repository, EmailService, TokenService, EventPublisher);
        }

        /// <summary>A confirmed, active customer - the happy-path user.</summary>
        protected static User ActiveUser(
            string id = "user-1",
            string email = "john.doe@example.com",
            string name = "John Doe",
            Role role = Role.Customer,
            bool isEmailConfirmed = true) => new()
            {
                Id = id,
                Email = email,
                Name = name,
                Role = role,
                IsActive = true,
                IsEmailConfirmed = isEmailConfirmed
            };

        /// <summary>A user whose email has not been confirmed yet.</summary>
        protected static User UnconfirmedUser(string id = "user-1", string email = "john.doe@example.com") =>
            ActiveUser(id, email, isEmailConfirmed: false);
    }
}
