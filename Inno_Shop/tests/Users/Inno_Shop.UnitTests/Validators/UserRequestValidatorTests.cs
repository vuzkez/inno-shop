using FluentValidation;
using Inno_Shop.Users.Application.Dtos;
using Inno_Shop.Users.Application.Validators;

namespace Inno_Shop.UnitTests.Validators
{
    /// <summary>
    /// The FluentValidation rules are part of the business contract: they decide what
    /// reaches the repository at all. Each validator is exercised directly, with no
    /// web host involved.
    /// </summary>
    public class RegisterRequestValidatorTests
    {
        private readonly RegisterRequestValidator _validator = new();

        [Fact]
        public void ValidRequest_Passes()
        {
            var result = _validator.Validate(new RegisterRequest("John Doe", "john@example.com", "Passw0rd!"));

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("", "Name is required.")]
        [InlineData("   ", "Name is required.")]
        public void EmptyName_IsRejected(string name, string expectedMessage)
        {
            var result = _validator.Validate(new RegisterRequest(name, "john@example.com", "Passw0rd!"));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Name)
                                                && e.ErrorMessage == expectedMessage);
        }

        [Fact]
        public void NameLongerThan100Characters_IsRejected()
        {
            var result = _validator.Validate(
                new RegisterRequest(new string('a', 101), "john@example.com", "Passw0rd!"));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(RegisterRequest.Name))
                .Which.ErrorMessage.Should().Be("Name must not exceed 100 characters.");
        }

        [Fact]
        public void NameOfExactly100Characters_IsAccepted()
        {
            var result = _validator.Validate(
                new RegisterRequest(new string('a', 100), "john@example.com", "Passw0rd!"));

            result.IsValid.Should().BeTrue();
        }

        // Note: FluentValidation's EmailAddress rule follows a pragmatic grammar and
        // accepts addresses without a dot in the domain ("user@localhost"), so only
        // structurally malformed values are asserted here.
        [Theory]
        [InlineData("not-an-email")]
        [InlineData("no-at-sign.example.com")]
        [InlineData("@example.com")]
        [InlineData("john@")]
        public void MalformedEmail_IsRejected(string email)
        {
            var result = _validator.Validate(new RegisterRequest("John Doe", email, "Passw0rd!"));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Email));
        }

        [Fact]
        public void EmptyEmail_ReportsTheRequiredMessage()
        {
            var result = _validator.Validate(new RegisterRequest("John Doe", "", "Passw0rd!"));

            result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Email)
                                                && e.ErrorMessage == "Email is required.");
        }

        [Theory]
        [InlineData("short1A", "Password must be at least 8 characters.")]
        [InlineData("alllowercase1", "Password must contain at least one uppercase letter.")]
        [InlineData("ALLUPPERCASE1", "Password must contain at least one lowercase letter.")]
        [InlineData("NoDigitsHere", "Password must contain at least one digit.")]
        public void WeakPassword_IsRejectedWithASpecificReason(string password, string expectedMessage)
        {
            var result = _validator.Validate(new RegisterRequest("John Doe", "john@example.com", password));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Password)
                                                && e.ErrorMessage == expectedMessage);
        }

        [Fact]
        public void EmptyPassword_IsRejected()
        {
            var result = _validator.Validate(new RegisterRequest("John Doe", "john@example.com", ""));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Password));
        }

        [Fact]
        public void PasswordLongerThan128Characters_IsRejected()
        {
            var password = new string('a', 125) + "Aa1!";

            password.Should().HaveLength(129);
            var result = _validator.Validate(new RegisterRequest("John Doe", "john@example.com", password));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Password)
                                                && e.ErrorMessage == "Password must not exceed 128 characters.");
        }

        [Fact]
        public void PasswordOfExactly128Characters_IsAccepted()
        {
            var password = new string('a', 124) + "Aa1!";

            password.Should().HaveLength(128);
            var result = _validator.Validate(new RegisterRequest("John Doe", "john@example.com", password));

            result.IsValid.Should().BeTrue();
        }
    }

    public class CreateUserRequestValidatorTests
    {
        private readonly CreateUserRequestValidator _validator = new();

        [Theory]
        [InlineData("Customer")]
        [InlineData("Admin")]
        [InlineData("admin")]
        [InlineData("CUSTOMER")]
        public void KnownRole_Passes(string role)
        {
            var result = _validator.Validate(
                new CreateUserRequest("Jane Doe", "jane@example.com", "Passw0rd!", role));

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("SuperUser")]
        [InlineData("")]
        [InlineData("1")]
        public void UnknownOrEmptyRole_IsRejected(string role)
        {
            var result = _validator.Validate(
                new CreateUserRequest("Jane Doe", "jane@example.com", "Passw0rd!", role));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.Role));
        }

        [Fact]
        public void NumericRoleValueThatParsesAsEnum_IsStillRejected()
        {
            // "1" parses as Role.Admin by position, but the API contract is a role name.
            var result = _validator.Validate(
                new CreateUserRequest("Jane Doe", "jane@example.com", "Passw0rd!", "1"));

            result.IsValid.Should().BeFalse();
        }
    }

    public class UpdateProfileRequestValidatorTests
    {
        private readonly UpdateProfileRequestValidator _validator = new();

        [Fact]
        public void NameOnly_Passes()
        {
            var result = _validator.Validate(new UpdateProfileRequest("New Name", null));

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void EmailOnly_Passes()
        {
            var result = _validator.Validate(new UpdateProfileRequest(null, "new@example.com"));

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("   ", "   ")]
        public void EmptyRequest_IsRejected(string? name, string? email)
        {
            var result = _validator.Validate(new UpdateProfileRequest(name, email));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e =>
                e.ErrorMessage == "At least one of 'Name' or 'Email' must be provided.");
        }

        [Fact]
        public void InvalidEmailAlongsideValidName_IsRejected()
        {
            var result = _validator.Validate(new UpdateProfileRequest("New Name", "not-an-email"));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileRequest.Email));
        }

        [Fact]
        public void EmptyNameAlongsideValidEmail_IsRejected()
        {
            var result = _validator.Validate(new UpdateProfileRequest("", "new@example.com"));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileRequest.Name));
        }
    }

    public class LoginRequestValidatorTests
    {
        private readonly LoginRequestValidator _validator = new();

        [Fact]
        public void NonEmptyCredentials_Pass()
        {
            var result = _validator.Validate(new LoginRequest("john@example.com", "anything"));

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void EmptyPassword_IsRejected()
        {
            var result = _validator.Validate(new LoginRequest("john@example.com", ""));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequest.Password));
        }

        [Fact]
        public void EmptyEmail_IsRejected()
        {
            var result = _validator.Validate(new LoginRequest("", "Passw0rd!"));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequest.Email));
        }
    }

    public class ResetPasswordRequestValidatorTests
    {
        private readonly ResetPasswordRequestValidator _validator = new();

        [Fact]
        public void ValidRequest_Passes()
        {
            var result = _validator.Validate(
                new ResetPasswordRequest("john@example.com", "token", "NewPassw0rd!"));

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void EmptyToken_IsRejected()
        {
            var result = _validator.Validate(
                new ResetPasswordRequest("john@example.com", "", "NewPassw0rd!"));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(ResetPasswordRequest.Token)
                                                && e.ErrorMessage == "Token is required.");
        }

        [Fact]
        public void WeakNewPassword_IsRejected()
        {
            var result = _validator.Validate(
                new ResetPasswordRequest("john@example.com", "token", "weak"));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(ResetPasswordRequest.NewPassword));
        }
    }

    public class ConfirmEmailRequestValidatorTests
    {
        private readonly ConfirmEmailRequestValidator _validator = new();

        [Fact]
        public void ValidRequest_Passes()
        {
            var result = _validator.Validate(new ConfirmEmailRequest("user-1", "token"));

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void EmptyUserId_IsRejected()
        {
            var result = _validator.Validate(new ConfirmEmailRequest("", "token"));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(ConfirmEmailRequest.UserId));
        }

        [Fact]
        public void EmptyToken_IsRejected()
        {
            var result = _validator.Validate(new ConfirmEmailRequest("user-1", ""));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(ConfirmEmailRequest.Token));
        }
    }

    public class UserListQueryValidatorTests
    {
        private readonly UserListQueryValidator _validator = new();

        [Fact]
        public void DefaultQuery_Passes()
        {
            var result = _validator.Validate(new UserListQuery());

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void PageBelowOne_IsRejected(int page)
        {
            var result = _validator.Validate(new UserListQuery(Page: page));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(UserListQuery.Page));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public void PageSizeOutsideOneToHundred_IsRejected(int pageSize)
        {
            var result = _validator.Validate(new UserListQuery(PageSize: pageSize));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(UserListQuery.PageSize));
        }

        [Fact]
        public void SearchLongerThan100Characters_IsRejected()
        {
            var result = _validator.Validate(new UserListQuery(Search: new string('a', 101)));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(UserListQuery.Search));
        }

        [Fact]
        public void UnknownRoleFilter_IsRejected()
        {
            var result = _validator.Validate(new UserListQuery(Role: "SuperUser"));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(UserListQuery.Role));
        }

        [Fact]
        public void BlankRoleFilter_IsIgnored()
        {
            var result = _validator.Validate(new UserListQuery(Role: "   "));

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("Customer")]
        [InlineData("admin")]
        public void KnownRoleFilter_Passes(string role)
        {
            var result = _validator.Validate(new UserListQuery(Role: role));

            result.IsValid.Should().BeTrue();
        }
    }

    public class ForgotAndResendValidatorsTests
    {
        [Fact]
        public void ForgotPassword_WithValidEmail_Passes()
        {
            new ForgotPasswordRequestValidator()
                .Validate(new ForgotPasswordRequest("john@example.com"))
                .IsValid.Should().BeTrue();
        }

        [Fact]
        public void ForgotPassword_WithMalformedEmail_IsRejected()
        {
            new ForgotPasswordRequestValidator()
                .Validate(new ForgotPasswordRequest("not-an-email"))
                .IsValid.Should().BeFalse();
        }

        [Fact]
        public void ResendConfirmation_WithMalformedEmail_IsRejected()
        {
            new ResendConfirmationRequestValidator()
                .Validate(new ResendConfirmationRequest("not-an-email"))
                .IsValid.Should().BeFalse();
        }

        [Fact]
        public void RefreshToken_WithEmptyToken_IsRejected()
        {
            var result = new RefreshTokenRequestValidator().Validate(new RefreshTokenRequest(""));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(RefreshTokenRequest.RefreshToken));
        }

        [Fact]
        public void RefreshToken_WithTokenLongerThan512Characters_IsRejected()
        {
            var result = new RefreshTokenRequestValidator()
                .Validate(new RefreshTokenRequest(new string('t', 513)));

            result.IsValid.Should().BeFalse();
        }
    }
}
