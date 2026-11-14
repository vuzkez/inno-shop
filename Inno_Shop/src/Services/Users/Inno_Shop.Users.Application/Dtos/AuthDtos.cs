using System;
using System.Collections.Generic;
using System.Text;

namespace Inno_Shop.Users.Application.Dtos
{
    public enum CredentialsCheck
    {
        Valid,
        Invalid,
        LockedOut
    }

    public enum LoginStatus
    {
        Success,
        InvalidCredentials,
        EmailNotConfirmed,
        Deactivated,
        LockedOut
    }

    public record LoginResult(LoginStatus Status, TokenResponseDto? Tokens = null)
    {
        public bool IsSuccess => Status == LoginStatus.Success;
    }

}
