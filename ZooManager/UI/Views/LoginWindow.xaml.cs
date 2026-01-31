using System.Windows;
using System.Windows.Input;
using ZooManager.Core.Interfaces;

namespace ZooManager.UI.Views
{
    public partial class LoginWindow : Window
    {
        private readonly IAuthenticationService _authService;
        private readonly IPersistenceService _persistenceService;

        public LoginWindow(IAuthenticationService authService, IPersistenceService persistenceService)
        {
            InitializeComponent();
            _authService = authService;
            _persistenceService = persistenceService;

            UsernameTextBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) LoginButton_Click(null, null); };
            PasswordBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) LoginButton_Click(null, null); };
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessage.Visibility = Visibility.Collapsed;

            var username = UsernameTextBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Bitte geben Sie Benutzername und Passwort ein.");
                return;
            }

            var user = _authService.AuthenticateUser(username, password);
            if (user != null)
            {
                var mainWindow = new MainWindow(_persistenceService, _authService);
                mainWindow.Show();
                this.Close();
            }
            else
            {
                ShowError("Ungültige Anmeldedaten. Bitte versuchen Sie es erneut.");
                PasswordBox.Clear();
                UsernameTextBox.Focus();
            }
        }

        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
        }
    }
}