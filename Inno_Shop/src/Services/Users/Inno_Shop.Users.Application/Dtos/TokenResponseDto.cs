using System;
using System.Collections.Generic;
using System.Text;

namespace Inno_Shop.Users.Application.Dtos
{
    public record TokenResponseDto(string AccessToken, string RefreshToken);
}
