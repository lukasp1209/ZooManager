using System;
using System.Security.Cryptography;
using System.Text;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Models;

namespace ZooManager.Infrastructure.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IPersistenceService _persistenceService;
        private User? _currentUser;

        public AuthenticationService(IPersistenceService persistenceService)
        {
            _persistenceService = persistenceService;
        }

        public bool IsLoggedIn => _currentUser != null;

        public User? GetCurrentUser() => _currentUser;

        public User? AuthenticateUser(string username, string password)
        {
            var user = _persistenceService.GetUserByUsername(username);
            if (user != null && user.IsActive && VerifyPassword(password, user.PasswordHash))
            {
                _currentUser = user;
                return user;
            }
            return null;
        }

        public bool CreateUser(string username, string password, UserRole role, int? employeeId = null)
        {
            if (_persistenceService.GetUserByUsername(username) != null)
                return false; 

            var passwordHash = HashPassword(password);
            var user = new User
            {
                Username = username,
                PasswordHash = passwordHash,
                Role = role,
                EmployeeId = employeeId,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            return _persistenceService.SaveUser(user);
        }

        public bool ChangePassword(int userId, string oldPassword, string newPassword)
        {
            var user = _persistenceService.GetUserById(userId);
            if (user != null && VerifyPassword(oldPassword, user.PasswordHash))
            {
                user.PasswordHash = HashPassword(newPassword);
                return _persistenceService.SaveUser(user);
            }
            return false;
        }

        public void Logout()
        {
            _currentUser = null;
        }

        public bool HasPermission(string action)
        {
            if (_currentUser == null) return false;
            
            return _currentUser.Role switch
            {
                UserRole.Admin => true,
                UserRole.ZooManager => true,

                UserRole.Employee => action switch
                {
                    "ViewAnimals" => true,
                    "ViewFeedingPlan" => true,
                    "ViewEvents" => true,
                    "ConfirmFeeding" => true,
                    "AddAnimalEvent" => true,
                    _ => false
                },

                _ => false
            };
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "ZooManagerSalt"));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }
    }
}