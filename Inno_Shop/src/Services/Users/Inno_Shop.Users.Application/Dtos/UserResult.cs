using System;
using System.Collections.Generic;
using System.Text;

namespace Inno_Shop.Users.Application.Dtos
{
    public class UserResult
    {
        public bool IsSuccess { get; set; }
        public string? UserId { get; set; }
        public List<string> Errors { get; set; } = new();
        public static UserResult Success(string userId) => new() { IsSuccess = true, UserId = userId };
        public static UserResult Failure(List<string> errors) => new() { IsSuccess = false, Errors = errors };
        public static UserResult Failure(string error) => new() { IsSuccess = false, Errors = new List<string> { error } };
    }
}
