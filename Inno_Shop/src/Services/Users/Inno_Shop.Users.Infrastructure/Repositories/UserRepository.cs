using System;
using System.Collections.Generic;
using System.Text;
using Inno_Shop.Users.Application.Dtos;
using Inno_Shop.Users.Application.Interfaces;
using Inno_Shop.Users.Domain;
using Inno_Shop.Users.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Inno_Shop.Users.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserRepository(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
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

        public async Task<UserResult> CreateUserAsync(string name, string email, string password)
        {
            var appUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Name = name,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(appUser, password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return UserResult.Failure(errors);
            }

            await _userManager.AddToRoleAsync(appUser, Role.Customer.ToString());

            return UserResult.Success(appUser.Id);
        }

        public async Task<UserResult> UpdateUserAsync(User user)
        {
            var appUser = await _userManager.FindByIdAsync(user.Id);
            if (appUser == null) 
                return UserResult.Failure("User not found.");

            appUser.Name = user.Name;
            appUser.IsActive = user.IsActive;

            if (appUser.Email != user.Email)
            {
                appUser.Email = user.Email;
                appUser.UserName = user.Email;
                appUser.EmailConfirmed = false;
            }

            var result = await _userManager.UpdateAsync(appUser);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return UserResult.Failure(errors);
            }

            return UserResult.Success(appUser.Id);
        }

        public async Task<bool> CheckPasswordAsync(string email, string password)
        {
            var appUser = await _userManager.FindByEmailAsync(email);
            if (appUser == null) 
                return false;

            return await _userManager.CheckPasswordAsync(appUser, password);
        }

        public async Task<bool> DeactivateUserAsync(string id)
        {
            var appUser = await _userManager.FindByIdAsync(id);
            if (appUser == null) 
                return false;

            appUser.IsActive = false;
            var result = await _userManager.UpdateAsync(appUser);

            // TODO: Deactivate products

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
                return UserResult.Failure("User not found.");

            var result = await _userManager.ConfirmEmailAsync(appUser, token);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return UserResult.Failure(errors);
            }

            return UserResult.Success(appUser.Id);
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
                return UserResult.Failure("User not found.");

            var result = await _userManager.ResetPasswordAsync(appUser, token, newPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return UserResult.Failure(errors);
            }

            return UserResult.Success(appUser.Id);
        }
        private async Task<User> MapToDomainAsync(ApplicationUser appUser)
        {
            var roles = await _userManager.GetRolesAsync(appUser);
            var roleString = roles.FirstOrDefault() ?? Role.Customer.ToString();
            Enum.TryParse<Role>(roleString, out var domainRole);

            return new User
            {
                Id = appUser.Id,
                Email = appUser.Email = null!,
                Name = appUser.Name,
                IsActive = appUser.IsActive,
                IsEmailConfirmed = appUser.EmailConfirmed,
                Role = domainRole
            };
        }
    }
}
