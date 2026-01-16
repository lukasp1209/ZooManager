using System.Windows;
using System.Windows.Controls;
using ZooManager.Core.Models;

namespace ZooManager.UI.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            UpdateUI();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string role = btn.Tag.ToString();
                
                switch (role)
                {
                    case "Admin":
                        UserSession.CurrentUsername = "Master Admin";
                        UserSession.CurrentRole = UserRole.Admin;
                        break;
                    case "ZooManager":
                        UserSession.CurrentUsername = "Zoo Direktor";
                        UserSession.CurrentRole = UserRole.ZooManager;
                        break;
                    case "Employee":
                        UserSession.CurrentUsername = "Tierpfleger Max";
                        UserSession.CurrentRole = UserRole.Employee;
                        break;
                }

                UpdateUI();
                ZooMessageBox.Show($"Erfolgreich als {role} angemeldet.", "System Login");
            }
        }

        private void UpdateUI()
        {
            CurrentUserNameDisplay.Text = UserSession.CurrentUsername;
            CurrentUserRoleDisplay.Text = $"Rolle: {UserSession.CurrentRole}";
        }
    }
}