using System;
using System.Collections.Generic;
using System.Text;
using Inno_Shop.Users.Application.Dtos;
using Inno_Shop.Users.Domain;

namespace Inno_Shop.Users.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(string id);
        Task<User?> GetByEmailAsync(string email);
        Task<UserResult> CreateUserAsync(string name, string email, string password);
        Task<UserResult> UpdateUserAsync(User user);
        Task<bool> CheckPasswordAsync(string email, string password);
        Task<bool> DeactivateUserAsync(string id);
        Task<string> GenerateEmailConfirmationTokenAsync(string id);
        Task<UserResult> ConfirmEmailAsync(string id, string token);
        Task<string> GeneratePasswordResetTokenAsync(string email);
        Task<UserResult> ResetPasswordAsync(string email, string token, string newPassword);
    }
}
