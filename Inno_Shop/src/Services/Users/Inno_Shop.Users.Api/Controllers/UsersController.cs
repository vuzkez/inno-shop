using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Inno_Shop.Users.Application.Dtos;
using Inno_Shop.Users.Application.Interfaces;
using Inno_Shop.Users.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inno_Shop.Users.Api.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _users;

        public UsersController(IUserService users)
        {
            _users = users;
        }

        [HttpGet("me")]
        [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMe()
        {
            var profile = await _users.GetProfileAsync(CurrentUserId);
            return profile == null ? NotFound() : Ok(profile);
        }

        [HttpPut("me")]
        [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateMe(UpdateProfileRequest request)
        {
            var result = await _users.UpdateProfileAsync(CurrentUserId, request);
            if (!result.IsSuccess)
                return ToProblem(result);

            return Ok(await _users.GetProfileAsync(CurrentUserId));
        }

        [HttpPost("me/deactivate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeactivateMe()
        {
            var result = await _users.DeactivateAsync(CurrentUserId);
            return result.IsSuccess ? NoContent() : ToProblem(result);
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType<PagedResult<UserProfileDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] UserListQuery query) =>
            Ok(await _users.GetUsersAsync(query));

        [HttpGet("{id}")]
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(string id)
        {
            var profile = await _users.GetProfileAsync(id);
            return profile == null ? NotFound() : Ok(profile);
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType<UserProfileDto>(StatusCodes.Status201Created)]
        public async Task<IActionResult> Create(CreateUserRequest request)
        {
            var result = await _users.CreateUserAsync(request);
            if (!result.IsSuccess)
                return ToProblem(result);

            var profile = await _users.GetProfileAsync(result.UserId!);
            return CreatedAtAction(nameof(GetById), new { id = result.UserId }, profile);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(string id, UpdateProfileRequest request)
        {
            var result = await _users.UpdateProfileAsync(id, request);
            if (!result.IsSuccess)
                return ToProblem(result);

            return Ok(await _users.GetProfileAsync(id));
        }

        [HttpPost("{id}/deactivate")]
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Deactivate(string id)
        {
            var result = await _users.DeactivateAsync(id);
            return result.IsSuccess ? NoContent() : ToProblem(result);
        }

        [HttpPost("{id}/activate")]
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Activate(string id)
        {
            var result = await _users.ActivateAsync(id);
            return result.IsSuccess ? NoContent() : ToProblem(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _users.DeleteAsync(id, CurrentUserId);
            return result.IsSuccess ? NoContent() : ToProblem(result);
        }

        private string CurrentUserId =>
            User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("Token does not contain user id.");

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
