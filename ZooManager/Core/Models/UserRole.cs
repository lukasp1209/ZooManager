namespace ZooManager.Core.Models
{
    public enum UserRole { Admin, ZooManager, Employee }

    public static class UserSession
    {
        public static string CurrentUsername { get; set; } = "Gast";
        public static UserRole CurrentRole { get; set; } = UserRole.Employee;
    }
}