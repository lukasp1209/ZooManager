using System.Security.Cryptography;
using System.Text;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Models;

namespace ZooManager.Infrastructure.Authentication
{
    /// <summary>
    /// Handles user authentication, password management and role-based authorization.
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IPersistenceService _persistenceService;
        private User? _currentUser; // Currently logged-in user

        public AuthenticationService(IPersistenceService persistenceService)
        {
            _persistenceService = persistenceService;
        }

        // Indicates whether a user is logged in
        public bool IsLoggedIn => _currentUser != null;

        public User? GetCurrentUser() => _currentUser;

        public User? AuthenticateUser(string username, string password)
        {
            var user = _persistenceService.GetUserByUsername(username);

            // Validate user and password
            if (user != null && user.IsActive && VerifyPassword(password, user.PasswordHash))
            {
                _currentUser = user;
                return user;
            }

            return null;
        }

        public bool CreateUser(string username, string password, UserRole role, int? employeeId = null)
        {
            // Prevent duplicate usernames
            if (_persistenceService.GetUserByUsername(username) != null)
                return false;

            var user = new User
            {
                Username = username,
                PasswordHash = HashPassword(password),
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

            // Only update if old password is correct
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

        // Simple role-based permission check
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

        // Hash password using SHA256 with static salt
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(
                Encoding.UTF8.GetBytes(password + "ZooManagerSalt")
            );
            return Convert.ToBase64String(hashedBytes);
        }

        private static bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }
    }
}