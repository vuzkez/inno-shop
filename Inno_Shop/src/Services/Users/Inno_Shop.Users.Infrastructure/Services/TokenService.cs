using System.Security.Cryptography;
using System.Text;
using Inno_Shop.Users.Application.Dtos;
using Inno_Shop.Users.Application.Interfaces;
using Inno_Shop.Users.Application.Options;
using Inno_Shop.Users.Domain;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Inno_Shop.Users.Infrastructure.Services
{
    /// <summary>
    /// Refresh-токены хранятся в Redis только в виде SHA-256 хэша:
    ///   refresh:{hash}      -> userId (TTL = RefreshTokenExpiryDays)
    ///   user-refresh:{id}   -> множество хэшей токенов пользователя (чтобы отзывать все сразу)
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly IJwtProvider _jwtProvider;
        private readonly IDatabase _redisDb;
        private readonly JwtOptions _jwtOptions;
        private readonly IUserRepository _userRepository;

        public TokenService(
            IJwtProvider jwtProvider,
            IConnectionMultiplexer redis,
            IOptions<JwtOptions> jwtOptions,
            IUserRepository userRepository)
        {
            _jwtProvider = jwtProvider;
            _redisDb = redis.GetDatabase();
            _jwtOptions = jwtOptions.Value;
            _userRepository = userRepository;
        }

        public async Task<TokenResponseDto> GenerateTokensAsync(User user)
        {
            var accessToken = _jwtProvider.GenerateToken(user);
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            var hash = Hash(refreshToken);
            var expiry = TimeSpan.FromDays(_jwtOptions.RefreshTokenExpiryDays);

            await _redisDb.StringSetAsync(TokenKey(hash), user.Id, expiry);
            await _redisDb.SetAddAsync(UserKey(user.Id), hash);
            await _redisDb.KeyExpireAsync(UserKey(user.Id), expiry);

            return new TokenResponseDto(accessToken, refreshToken);
        }

        public async Task<TokenResponseDto> RefreshTokensAsync(string refreshToken)
        {
            var hash = Hash(refreshToken);

            var userId = await _redisDb.StringGetDeleteAsync(TokenKey(hash));
            if (userId.IsNullOrEmpty)
                throw new UnauthorizedAccessException("Invalid or expired Refresh Token.");

            await _redisDb.SetRemoveAsync(UserKey(userId!), hash);

            var user = await _userRepository.GetByIdAsync(userId!);
            if (user == null || !user.IsActive)
                throw new UnauthorizedAccessException("User not found or deactivated.");

            return await GenerateTokensAsync(user);
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var hash = Hash(refreshToken);
            var userId = await _redisDb.StringGetDeleteAsync(TokenKey(hash));
            if (!userId.IsNullOrEmpty)
                await _redisDb.SetRemoveAsync(UserKey(userId!), hash);
        }

        public async Task RevokeAllUserTokensAsync(string userId)
        {
            var hashes = await _redisDb.SetMembersAsync(UserKey(userId));
            if (hashes.Length > 0)
            {
                var keys = hashes.Select(h => (RedisKey)TokenKey(h.ToString())).ToArray();
                await _redisDb.KeyDeleteAsync(keys);
            }

            await _redisDb.KeyDeleteAsync(UserKey(userId));
        }

        private static string TokenKey(string hash) => $"refresh:{hash}";
        private static string UserKey(string userId) => $"user-refresh:{userId}";

        private static string Hash(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
