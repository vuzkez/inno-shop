using Inno_Shop.Users.Application.Dtos;
using Inno_Shop.Users.Domain;

namespace Inno_Shop.Users.Application.Interfaces
{
    public interface IUserRepository
    {
        /// <summary>Finds a user by id.</summary>
        /// <returns>The user, or null if not found.</returns>
        Task<User?> GetByIdAsync(string id);

        /// <summary>Finds a user by email (case-insensitive).</summary>
        /// <returns>The user, or null if not found.</returns>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// Returns a page of users with search by name/email and filters by role and active status.
        /// Used by administrators to browse users.
        /// </summary>
        /// <param name="query">Search, filter and paging parameters.</param>
        Task<PagedResult<User>> GetUsersAsync(UserListQuery query);

        /// <summary>
        /// Creates a user with a password and assigns a role.
        /// Normal registration: role Customer, email not confirmed.
        /// Creation by an admin: any role, email confirmed immediately.
        /// </summary>
        /// <param name="name">Display name.</param>
        /// <param name="email">Email (also the login, must be unique).</param>
        /// <param name="password">Plain-text password; only its hash is stored in the database.</param>
        /// <param name="role">The user's role.</param>
        /// <param name="emailConfirmed">Whether the email should be treated as already confirmed.</param>
        /// <returns>Success with the new user's id, or an error (for example Conflict if the email is taken).</returns>
        Task<UserResult> CreateUserAsync(string name, string email, string password, Role role = Role.Customer, bool emailConfirmed = false);

        /// <summary>
        /// Saves a changed name and/or email. Changing the email resets its confirmation.
        /// A deactivated user cannot be updated.
        /// </summary>
        /// <param name="user">The user with the new field values.</param>
        Task<UserResult> UpdateUserAsync(User user);

        /// <summary>Permanently deletes the user from the database.</summary>
        /// <returns>true if deleted; false if the user was not found or deletion failed.</returns>
        Task<bool> DeleteUserAsync(string id);

        /// <summary>
        /// Verifies email and password with brute-force protection: every wrong attempt
        /// increments a counter, and after 5 failures the account is locked for 5 minutes.
        /// </summary>
        /// <returns>Valid - correct password; Invalid - wrong password; LockedOut - account is locked.</returns>
        Task<CredentialsCheck> CheckCredentialsAsync(string email, string password);

        /// <summary>
        /// Deactivates the account (a soft disable that keeps the data): the user cannot log in
        /// until an admin activates the account again. Calling it repeatedly is safe.
        /// </summary>
        /// <returns>false if the user was not found.</returns>
        Task<bool> DeactivateUserAsync(string id);

        /// <summary>Re-enables a previously deactivated account. Calling it repeatedly is safe.</summary>
        /// <returns>false if the user was not found.</returns>
        Task<bool> ActivateUserAsync(string id);

        /// <summary>Generates the token used in the email-confirmation message.</summary>
        /// <returns>The token, or an empty string if the user was not found.</returns>
        Task<string> GenerateEmailConfirmationTokenAsync(string id);

        /// <summary>
        /// Confirms the email using the token from the message.
        /// After that the user is allowed to log in.
        /// </summary>
        /// <param name="id">User id taken from the link.</param>
        /// <param name="token">Token taken from the link.</param>
        Task<UserResult> ConfirmEmailAsync(string id, string token);

        /// <summary>Generates the token used in the password-reset message.</summary>
        /// <returns>The token, or an empty string if no user has this email.</returns>
        Task<string> GeneratePasswordResetTokenAsync(string email);

        /// <summary>
        /// Sets a new password using the token from the message. The password is checked against
        /// the policy (length, uppercase and lowercase letters, digit).
        /// </summary>
        /// <param name="email">The user's email.</param>
        /// <param name="token">Token from the message.</param>
        /// <param name="newPassword">The new password.</param>
        Task<UserResult> ResetPasswordAsync(string email, string token, string newPassword);
    }
}