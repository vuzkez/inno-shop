using Inno_Shop.Users.Application.Dtos;

namespace Inno_Shop.Users.Application.Interfaces
{
    public interface IUserService
    {
        /// <summary>
        /// Registers a new user (role Customer) and sends the email-confirmation message.
        /// </summary>
        /// <returns>Success with the user id, or an error (for example the email is already taken).</returns>
        Task<UserResult> RegisterAsync(RegisterRequest request);

        /// <summary>
        /// Logs in with email and password. Tokens are issued only if the password is correct,
        /// the account is not locked or deactivated, and the email is confirmed.
        /// </summary>
        /// <returns>The login status (success or the reason for refusal) and the tokens on success.</returns>
        Task<LoginResult> LoginAsync(LoginRequest request);

        /// <summary>Renews the token pair using a refresh token, without asking for the password.</summary>
        /// <returns>New tokens, or null if the refresh token is invalid.</returns>
        Task<TokenResponseDto?> RefreshTokenAsync(string refreshToken);

        /// <summary>Logout: invalidates the refresh token.</summary>
        Task LogoutAsync(string refreshToken);

        /// <summary>
        /// Confirms the email using the link from the message. On any error it returns the same text,
        /// so nobody can find out whether such a user exists.
        /// </summary>
        Task<UserResult> ConfirmEmailAsync(ConfirmEmailRequest request);

        /// <summary>
        /// Sends the confirmation email again (if it got lost or SMTP was temporarily down).
        /// Does nothing, and reveals nothing, if the account does not exist, is already confirmed or is deactivated.
        /// </summary>
        Task ResendConfirmationAsync(string email);

        /// <summary>
        /// Password recovery, step 1: sends an email with the reset link.
        /// Silently does nothing if the account does not exist or is deactivated (protects against address enumeration).
        /// </summary>
        Task SendForgotPasswordEmailAsync(string email);

        /// <summary>
        /// Password recovery, step 2: sets a new password using the token from the email
        /// and terminates all of the user's active sessions.
        /// </summary>
        Task<UserResult> ResetPasswordAsync(ResetPasswordRequest request);

        /// <summary>Returns a user's profile (Read).</summary>
        /// <returns>The profile, or null if the user was not found.</returns>
        Task<UserProfileDto?> GetProfileAsync(string userId);

        /// <summary>Paged list of users with search and filters (for administrators).</summary>
        Task<PagedResult<UserProfileDto>> GetUsersAsync(UserListQuery query);

        /// <summary>
        /// Creates a user by an administrator (Create): sets the role, and the email is treated as confirmed.
        /// </summary>
        Task<UserResult> CreateUserAsync(CreateUserRequest request);

        /// <summary>
        /// Changes the name and/or email (Update). If the email changed it becomes unconfirmed again
        /// and a confirmation message is sent to the new address.
        /// </summary>
        /// <param name="userId">Whose profile is being changed.</param>
        /// <param name="request">New values; fields that are not provided stay unchanged.</param>
        Task<UserResult> UpdateProfileAsync(string userId, UpdateProfileRequest request);

        /// <summary>
        /// Deactivates a user: blocks login, terminates their sessions and notifies
        /// </summary>
        Task<UserResult> DeactivateAsync(string userId);

        /// <summary>
        /// Activates a user again and notifies
        /// </summary>
        Task<UserResult> ActivateAsync(string userId);

        /// <summary>
        /// Deletes a user permanently (Delete), terminates their sessions
        /// You cannot delete yourself.
        /// </summary>
        /// <param name="userId">Who is being deleted.</param>
        /// <param name="actorId">Who performs the deletion (taken from the token).</param>
        Task<UserResult> DeleteAsync(string userId, string actorId);
    }
}