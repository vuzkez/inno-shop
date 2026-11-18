using Inno_Shop.Users.Application.Dtos;
using Inno_Shop.Users.Application.Interfaces;
using Inno_Shop.Users.Domain;
using Inno_Shop.Users.Infrastructure.Identity;
using Inno_Shop.Users.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Inno_Shop.Users.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UsersDbContext _db;

        public UserRepository(UserManager<ApplicationUser> userManager, UsersDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public async Task<User?> GetByIdAsync(string id)
        {
            var appUser = await _userManager.FindByIdAsync(id);
            return appUser == null ? null : await MapToDomainAsync(appUser);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var appUser = await _userManager.FindByEmailAsync(email);
            return appUser == null ? null : await MapToDomainAsync(appUser);
        }

        public async Task<PagedResult<User>> GetUsersAsync(UserListQuery query)
        {
            var usersQuery = _db.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                usersQuery = usersQuery.Where(u =>
                    u.Name.Contains(search) ||
                    (u.Email != null && u.Email.Contains(search)));
            }

            if (query.IsActive.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.IsActive == query.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Role) && Enum.TryParse<Role>(query.Role, true, out var roleFilter))
            {
                var roleName = roleFilter.ToString();
                var userIdsInRole = _db.UserRoles
                    .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                    .Where(x => x.Name == roleName)
                    .Select(x => x.UserId);

                usersQuery = usersQuery.Where(u => userIdsInRole.Contains(u.Id));
            }

            var total = await usersQuery.CountAsync();
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var appUsers = await usersQuery
                .OrderBy(u => u.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userIds = appUsers.Select(u => u.Id).ToList();

            var roleAssignments = await _db.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .ToListAsync();

            var roleByUser = new Dictionary<string, string>();
            foreach (var assignment in roleAssignments)
            {
                // A role name is optional on the Identity entity; skipping null keeps the previous
                // behaviour, where a missing entry made MapToDomain fall back to Role.Customer.
                if (!string.IsNullOrEmpty(assignment.Name))
                    roleByUser.TryAdd(assignment.UserId, assignment.Name);
            }

            var items = appUsers
                .Select(u => MapToDomain(u, roleByUser.GetValueOrDefault(u.Id)))
                .ToList();

            return new PagedResult<User>(items, page, pageSize, total);
        }


        public async Task<UserResult> CreateUserAsync(string name, string email, string password, Role role = Role.Customer, bool emailConfirmed = false)
        {
            var appUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Name = name,
                IsActive = true,
                EmailConfirmed = emailConfirmed
            };

            var result = await _userManager.CreateAsync(appUser, password);
            if (!result.Succeeded)
                return ToFailure(result);

            var roleResult = await _userManager.AddToRoleAsync(appUser, role.ToString());
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(appUser);
                return ToFailure(roleResult);
            }

            return UserResult.Success(appUser.Id);
        }

        public async Task<UserResult> UpdateUserAsync(User user)
        {
            var appUser = await _userManager.FindByIdAsync(user.Id);
            if (appUser == null)
                return UserResult.Failure("User not found.", ErrorType.NotFound);

            if (!appUser.IsActive)
                return UserResult.Failure("Cannot update profile. User account is deactivated.", ErrorType.Forbidden);

            if (!string.IsNullOrWhiteSpace(user.Email)
                && !string.Equals(appUser.Email, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailResult = await _userManager.SetEmailAsync(appUser, user.Email);
                if (!emailResult.Succeeded)
                    return ToFailure(emailResult);

                var userNameResult = await _userManager.SetUserNameAsync(appUser, user.Email);
                if (!userNameResult.Succeeded)
                    return ToFailure(userNameResult);
            }

            if (!string.IsNullOrWhiteSpace(user.Name) && appUser.Name != user.Name)
            {
                appUser.Name = user.Name;
                var updateResult = await _userManager.UpdateAsync(appUser);
                if (!updateResult.Succeeded)
                    return ToFailure(updateResult);
            }

            return UserResult.Success(appUser.Id);
        }

        public async Task<bool> DeleteUserAsync(string id)
        {
            var appUser = await _userManager.FindByIdAsync(id);
            if (appUser == null)
                return false;

            var result = await _userManager.DeleteAsync(appUser);
            return result.Succeeded;
        }

        public async Task<CredentialsCheck> CheckCredentialsAsync(string email, string password)
        {
            var appUser = await _userManager.FindByEmailAsync(email);
            if (appUser == null)
                return CredentialsCheck.Invalid;

            if (await _userManager.IsLockedOutAsync(appUser))
                return CredentialsCheck.LockedOut;

            if (await _userManager.CheckPasswordAsync(appUser, password))
            {
                await _userManager.ResetAccessFailedCountAsync(appUser);
                return CredentialsCheck.Valid;
            }

            await _userManager.AccessFailedAsync(appUser);
            return await _userManager.IsLockedOutAsync(appUser)
                ? CredentialsCheck.LockedOut
                : CredentialsCheck.Invalid;
        }

        public async Task<bool> DeactivateUserAsync(string id)
        {
            var appUser = await _userManager.FindByIdAsync(id);
            if (appUser == null)
                return false;

            if (!appUser.IsActive)
                return true;

            appUser.IsActive = false;
            var result = await _userManager.UpdateAsync(appUser);
            return result.Succeeded;
        }

        public async Task<bool> ActivateUserAsync(string id)
        {
            var appUser = await _userManager.FindByIdAsync(id);
            if (appUser == null)
                return false;

            if (appUser.IsActive)
                return true;

            appUser.IsActive = true;
            var result = await _userManager.UpdateAsync(appUser);
            return result.Succeeded;
        }

        public async Task<string> GenerateEmailConfirmationTokenAsync(string id)
        {
            var appUser = await _userManager.FindByIdAsync(id);
            if (appUser == null)
                return string.Empty;

            return await _userManager.GenerateEmailConfirmationTokenAsync(appUser);
        }

        public async Task<UserResult> ConfirmEmailAsync(string id, string token)
        {
            var appUser = await _userManager.FindByIdAsync(id);
            if (appUser == null)
                return UserResult.Failure("User not found.", ErrorType.NotFound);

            var result = await _userManager.ConfirmEmailAsync(appUser, token);
            return result.Succeeded ? UserResult.Success(appUser.Id) : ToFailure(result);
        }

        public async Task<string> GeneratePasswordResetTokenAsync(string email)
        {
            var appUser = await _userManager.FindByEmailAsync(email);
            if (appUser == null)
                return string.Empty;

            return await _userManager.GeneratePasswordResetTokenAsync(appUser);
        }

        public async Task<UserResult> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var appUser = await _userManager.FindByEmailAsync(email);
            if (appUser == null)
                return UserResult.Failure("User not found.", ErrorType.NotFound);

            var result = await _userManager.ResetPasswordAsync(appUser, token, newPassword);
            return result.Succeeded ? UserResult.Success(appUser.Id) : ToFailure(result);
        }

        private async Task<User> MapToDomainAsync(ApplicationUser appUser)
        {
            var roles = await _userManager.GetRolesAsync(appUser);
            return MapToDomain(appUser, roles.FirstOrDefault());
        }

        private static User MapToDomain(ApplicationUser appUser, string? roleName)
        {
            var role = Enum.TryParse<Role>(roleName, out var parsed) ? parsed : Role.Customer;

            return new User
            {
                Id = appUser.Id,
                Email = appUser.Email ?? string.Empty,
                Name = appUser.Name,
                IsActive = appUser.IsActive,
                IsEmailConfirmed = appUser.EmailConfirmed,
                Role = role
            };
        }

        private static UserResult ToFailure(IdentityResult result)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            var isConflict = result.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName");
            return UserResult.Failure(errors, isConflict ? ErrorType.Conflict : ErrorType.Validation);
        }
    }
}
