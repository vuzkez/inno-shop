using System;
using System.Collections.Generic;
using System.Text;

namespace Inno_Shop.Users.Application.Dtos
{
    public record RegisterRequest(string Name, string Email, string Password);
    public record LoginRequest(string Email, string Password);
    public record RefreshTokenRequest(string RefreshToken);
    public record UpdateProfileRequest(string? Name, string? Email);
    public record ConfirmEmailRequest(string UserId, string Token);
    public record ResendConfirmationRequest(string Email);
    public record ForgotPasswordRequest(string Email);
    public record ResetPasswordRequest(string Email, string Token, string NewPassword);
    public record CreateUserRequest(string Name, string Email, string Password, string Role);
    public record UserListQuery(string? Search = null, string? Role = null, bool? IsActive = null, int Page = 1, int PageSize = 20);
    public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
    {
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
    public record UserProfileDto(string Id, string Email, string Name, string Role, bool IsActive, bool IsEmailConfirmed);

}
