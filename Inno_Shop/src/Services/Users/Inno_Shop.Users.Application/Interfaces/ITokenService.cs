using Inno_Shop.Users.Application.Dtos;
using Inno_Shop.Users.Domain;

namespace Inno_Shop.Users.Application.Interfaces
{
    public interface ITokenService
    {
        /// <summary>
        /// Issues a new token pair on login: an access token (JWT) and a refresh token,
        /// which is saved in the store with an expiration time.
        /// </summary>
        /// <param name="user">The user the tokens are issued to.</param>
        Task<TokenResponseDto> GenerateTokensAsync(User user);

        /// <summary>
        /// Exchanges a refresh token for a new token pair, so the user does not have to
        /// enter their password every 15 minutes. The old refresh token is consumed (single use).
        /// </summary>
        /// <param name="refreshToken">The refresh token received at login or at the previous refresh.</param>
        /// <exception cref="UnauthorizedAccessException">
        /// The token is unknown or expired, or the user was deleted or deactivated.
        /// </exception>
        Task<TokenResponseDto> RefreshTokensAsync(string refreshToken);

        /// <summary>
        /// Revokes a single refresh token (logout from the current device).
        /// </summary>
        /// <param name="refreshToken">The token to invalidate.</param>
        Task RevokeRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Revokes ALL refresh tokens of a user (logout from every device).
        /// Called on deactivation, account deletion and password reset.
        /// </summary>
        /// <param name="userId">Id of the user whose sessions must be terminated.</param>
        Task RevokeAllUserTokensAsync(string userId);
    }
}