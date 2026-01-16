using System.Windows;
using ZooManager.Core.Models;

namespace ZooManager.UI.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            UsernameInput.Focus();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string user = UsernameInput.Text.ToLower();
            string pass = PasswordInput.Password;

            // Einfache Logik für die Rollen (In Realität DB-Abgleich)
            if (user == "admin" && pass == "admin")
            {
                UserSession.CurrentUsername = "Administrator";
                UserSession.CurrentRole = UserRole.Admin;
            }
            else if (user == "manager" && pass == "manager")
            {
                UserSession.CurrentUsername = "Zoo Direktor";
                UserSession.CurrentRole = UserRole.ZooManager;
            }
            else if (user == "pfleger" && pass == "pfleger")
            {
                UserSession.CurrentUsername = "Tierpfleger Max";
                UserSession.CurrentRole = UserRole.Employee;
            }
            else
            {
                ZooMessageBox.Show("Ungültige Zugangsdaten!", "Login Fehler");
                return;
            }

            // Erfolg: MainWindow öffnen und Login schließen
            var main = new MainWindow();
            main.Show();
            this.Close();
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}