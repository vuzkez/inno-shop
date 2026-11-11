using System;
using System.Collections.Generic;
using System.Text;
using Inno_Shop.Users.Application.Dtos;
using Inno_Shop.Users.Domain;

namespace Inno_Shop.Users.Application.Interfaces
{
    public interface ITokenService
    {
        Task<TokenResponseDto> GenerateTokensAsync(User user);
        Task<TokenResponseDto> RefreshTokensAsync(string refreshToken);
        Task RevokeRefreshTokenAsync(string refreshToken);
    }
}
