using Inno_Shop.Users.Application.Dtos;
using Inno_Shop.Users.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inno_Shop.Users.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _users;

        public AuthController(IUserService users)
        {
            _users = users;
        }

        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var result = await _users.RegisterAsync(request);
            if (!result.IsSuccess)
                return ToProblem(result);

            return StatusCode(StatusCodes.Status201Created, new
            {
                userId = result.UserId,
                message = "Account created. Please confirm your email."
            });
        }

        [HttpPost("login")]
        [ProducesResponseType<TokenResponseDto>(StatusCodes.Status200OK)]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var result = await _users.LoginAsync(request);

            return result.Status switch
            {
                LoginStatus.Success => Ok(result.Tokens),
                LoginStatus.EmailNotConfirmed => Problem(
                    title: "Email is not confirmed", detail: "Please confirm your email before logging in.",
                    statusCode: StatusCodes.Status403Forbidden),
                LoginStatus.Deactivated => Problem(
                    title: "Account is deactivated", detail: "Your account has been deactivated.",
                    statusCode: StatusCodes.Status403Forbidden),
                LoginStatus.LockedOut => Problem(
                    title: "Account is locked", detail: "Too many failed attempts. Try again later.",
                    statusCode: StatusCodes.Status423Locked),
                _ => Problem(
                    title: "Invalid credentials", detail: "Invalid email or password.",
                    statusCode: StatusCodes.Status401Unauthorized)
            };
        }

        [HttpPost("refresh")]
        [ProducesResponseType<TokenResponseDto>(StatusCodes.Status200OK)]
        public async Task<IActionResult> Refresh(RefreshTokenRequest request)
        {
            var tokens = await _users.RefreshTokenAsync(request.RefreshToken);
            return tokens == null
                ? Problem(title: "Invalid refresh token", detail: "Invalid or expired refresh token.",
                    statusCode: StatusCodes.Status401Unauthorized)
                : Ok(tokens);
        }

        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout(RefreshTokenRequest request)
        {
            await _users.LogoutAsync(request.RefreshToken);
            return NoContent();
        }

        [HttpGet("confirm-email")]
        public Task<IActionResult> ConfirmEmailByLink([FromQuery] ConfirmEmailRequest request) => ConfirmEmailCore(request);

        [HttpPost("confirm-email")]
        public Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request) => ConfirmEmailCore(request);

        private async Task<IActionResult> ConfirmEmailCore(ConfirmEmailRequest request)
        {
            var result = await _users.ConfirmEmailAsync(request);
            return result.IsSuccess ? Ok(new { message = "Email confirmed." }) : ToProblem(result);
        }

        [HttpPost("resend-confirmation")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public async Task<IActionResult> ResendConfirmation(ResendConfirmationRequest request)
        {
            await _users.ResendConfirmationAsync(request.Email);
            return Accepted(new { message = "If the account exists and is not confirmed, an email has been sent." });
        }

        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            await _users.SendForgotPasswordEmailAsync(request.Email);
            return Accepted(new { message = "If the account exists, a password reset email has been sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            var result = await _users.ResetPasswordAsync(request);
            return result.IsSuccess ? Ok(new { message = "Password has been reset." }) : ToProblem(result);
        }

        private IActionResult ToProblem(UserResult result)
        {
            var (status, title) = result.Type switch
            {
                ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
                ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
                ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
                ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
                _ => (StatusCodes.Status400BadRequest, "Bad Request")
            };

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = string.Join(" ", result.Errors)
            };
            problem.Extensions["errors"] = result.Errors;

            return new ObjectResult(problem) { StatusCode = status };
        }
    }
}
