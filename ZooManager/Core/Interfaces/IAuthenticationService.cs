using ZooManager.Core.Models;

namespace ZooManager.Core.Interfaces
{
    public interface IAuthenticationService
    {
        User? AuthenticateUser(string username, string password);
        bool CreateUser(string username, string password, UserRole role, int? employeeId = null);
        bool ChangePassword(int userId, string oldPassword, string newPassword);
        User? GetCurrentUser();
        void Logout();
        bool IsLoggedIn { get; }
        bool HasPermission(string action);
    }
}