using System;
using System.Collections.Generic;
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

            var key = $"refresh:{refreshToken}";
            var expiry = TimeSpan.FromDays(_jwtOptions.RefreshTokenExpiryDays);

            await _redisDb.StringSetAsync(key, user.Id, expiry);

            return new TokenResponseDto(accessToken, refreshToken);
        }

        public async Task<TokenResponseDto> RefreshTokensAsync(string refreshToken)
        {
            var key = $"refresh:{refreshToken}";
            var userId = await _redisDb.StringGetAsync(key);

            if (userId.IsNullOrEmpty)
                throw new UnauthorizedAccessException("Invalid or expired Refresh Token.");

            await _redisDb.KeyDeleteAsync(key);

            var user = await _userRepository.GetByIdAsync(userId!);
            if (user == null || !user.IsActive)
                throw new UnauthorizedAccessException("User not found or deactivated.");

            return await GenerateTokensAsync(user);
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            await _redisDb.KeyDeleteAsync($"refresh:{refreshToken}");
        }
    }
}
