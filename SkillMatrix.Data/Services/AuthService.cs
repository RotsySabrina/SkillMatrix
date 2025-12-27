using Microsoft.AspNetCore.Identity;
using SkillMatrix.Core.Models;

namespace SkillMatrix.Data.Services
{
    public class AuthService
    {
        private readonly PasswordHasher<User> _hasher = new PasswordHasher<User>();

        public string HashPassword(User user, string password) 
            => _hasher.HashPassword(user, password);

        public bool VerifyPassword(User user, string password)
        {
            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            return result == PasswordVerificationResult.Success;
        }
    }
}