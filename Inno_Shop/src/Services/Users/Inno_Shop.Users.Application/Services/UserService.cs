using Inno_Shop.Users.Application.Dtos;
using Inno_Shop.Users.Application.Interfaces;
using Inno_Shop.Users.Domain;

namespace Inno_Shop.Users.Application.Services
{
    public class UserService : IUserService
    {
        private const string InvalidTokenMessage = "Invalid or expired token.";

        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly ITokenService _tokenService;

        public UserService(
            IUserRepository userRepository,
            IEmailService emailService,
            ITokenService tokenService)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _tokenService = tokenService;
        }

        public async Task<UserResult> RegisterAsync(RegisterRequest request)
        {
            var email = request.Email.Trim();
            var result = await _userRepository.CreateUserAsync(request.Name.Trim(), email, request.Password);
            if (!result.IsSuccess)
                return result;

            await SendConfirmationAsync(result.UserId!, email);
            return result;
        }

        public async Task<LoginResult> LoginAsync(LoginRequest request)
        {
            var email = request.Email.Trim();

            var check = await _userRepository.CheckCredentialsAsync(email, request.Password);
            if (check == CredentialsCheck.LockedOut)
                return new LoginResult(LoginStatus.LockedOut);
            if (check != CredentialsCheck.Valid)
                return new LoginResult(LoginStatus.InvalidCredentials);

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                return new LoginResult(LoginStatus.InvalidCredentials);
            if (!user.IsActive)
                return new LoginResult(LoginStatus.Deactivated);
            if (!user.IsEmailConfirmed)
                return new LoginResult(LoginStatus.EmailNotConfirmed);

            var tokens = await _tokenService.GenerateTokensAsync(user);
            return new LoginResult(LoginStatus.Success, tokens);
        }

        public async Task<TokenResponseDto?> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                return await _tokenService.RefreshTokensAsync(refreshToken);
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        public Task LogoutAsync(string refreshToken) => _tokenService.RevokeRefreshTokenAsync(refreshToken);

        public async Task<UserResult> ConfirmEmailAsync(ConfirmEmailRequest request)
        {
            var result = await _userRepository.ConfirmEmailAsync(request.UserId, request.Token);
            return result.IsSuccess ? result : UserResult.Failure(InvalidTokenMessage);
        }

        public async Task ResendConfirmationAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email.Trim());
            if (user == null || !user.IsActive || user.IsEmailConfirmed)
                return;

            await SendConfirmationAsync(user.Id, user.Email);
        }

        public async Task SendForgotPasswordEmailAsync(string email)
        {
            var normalized = email.Trim();
            var user = await _userRepository.GetByEmailAsync(normalized);
            if (user == null || !user.IsActive)
                return;

            var token = await _userRepository.GeneratePasswordResetTokenAsync(normalized);
            if (!string.IsNullOrEmpty(token))
                await _emailService.SendPasswordResetAsync(normalized, token);
        }

        public async Task<UserResult> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var result = await _userRepository.ResetPasswordAsync(request.Email.Trim(), request.Token, request.NewPassword);
            if (!result.IsSuccess)
            {
                return result.Type == ErrorType.NotFound ? UserResult.Failure(InvalidTokenMessage) : result;
            }

            await _tokenService.RevokeAllUserTokensAsync(result.UserId!);
            return result;
        }

        public async Task<UserProfileDto?> GetProfileAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return user == null ? null : ToDto(user);
        }

        public async Task<PagedResult<UserProfileDto>> GetUsersAsync(UserListQuery query)
        {
            var page = await _userRepository.GetUsersAsync(query);
            return new PagedResult<UserProfileDto>(
                page.Items.Select(ToDto).ToList(), page.Page, page.PageSize, page.TotalCount);
        }

        public async Task<UserResult> CreateUserAsync(CreateUserRequest request)
        {
            var role = Enum.Parse<Role>(request.Role, ignoreCase: true);
            return await _userRepository.CreateUserAsync(
                request.Name.Trim(), request.Email.Trim(), request.Password, role, emailConfirmed: true);
        }

        public async Task<UserResult> UpdateProfileAsync(string userId, UpdateProfileRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return UserResult.Failure("User not found.", ErrorType.NotFound);

            var emailChanged = !string.IsNullOrWhiteSpace(request.Email)
                && !string.Equals(user.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(request.Name))
                user.Name = request.Name.Trim();

            if (emailChanged)
                user.Email = request.Email!.Trim();

            var result = await _userRepository.UpdateUserAsync(user);

            if (result.IsSuccess && emailChanged)
                await SendConfirmationAsync(user.Id, user.Email);

            return result;
        }

        public async Task<UserResult> DeactivateAsync(string userId)
        {
            if (!await _userRepository.DeactivateUserAsync(userId))
                return UserResult.Failure("User not found.", ErrorType.NotFound);

            await _tokenService.RevokeAllUserTokensAsync(userId);

            // TODO: deactive products

            return UserResult.Success(userId);
        }

        public async Task<UserResult> ActivateAsync(string userId)
        {
            if (!await _userRepository.ActivateUserAsync(userId))
                return UserResult.Failure("User not found.", ErrorType.NotFound);

            // TODO: active products

            return UserResult.Success(userId);
        }

        public async Task<UserResult> DeleteAsync(string userId, string actorId)
        {
            if (userId == actorId)
                return UserResult.Failure("You cannot delete your own account.", ErrorType.Forbidden);

            if (!await _userRepository.DeleteUserAsync(userId))
                return UserResult.Failure("User not found.", ErrorType.NotFound);

            await _tokenService.RevokeAllUserTokensAsync(userId);

            return UserResult.Success(userId);
        }

        private async Task SendConfirmationAsync(string userId, string email)
        {
            var token = await _userRepository.GenerateEmailConfirmationTokenAsync(userId);
            if (!string.IsNullOrEmpty(token))
                await _emailService.SendEmailConfirmationAsync(email, userId, token);
        }

        private static UserProfileDto ToDto(User user) =>
            new(user.Id, user.Email, user.Name, user.Role.ToString(), user.IsActive, user.IsEmailConfirmed);
    }
}