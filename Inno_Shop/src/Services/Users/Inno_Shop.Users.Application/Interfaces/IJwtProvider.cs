using Inno_Shop.Users.Domain;

namespace Inno_Shop.Users.Application.Interfaces
{
    public interface IJwtProvider
    {
        /// <summary>
        /// Creates a signed JWT containing the user's data (claims: sub, email, name, role, jti)
        /// with a limited lifetime (JwtSettings:ExpiryMinutes).
        /// The Products service uses this token to find out who is calling and what role they have.
        /// </summary>
        /// <param name="user">The user the token is issued for.</param>
        /// <returns>The JWT string the client sends in the "Authorization: Bearer" header.</returns>
        string GenerateToken(User user);
    }
}