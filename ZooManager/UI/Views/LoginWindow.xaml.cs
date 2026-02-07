using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using ZooManager.Core.Interfaces;
using ZooManager.UI.ViewModels;

namespace ZooManager.UI.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginWindowViewModel _vm;

        public LoginWindow(IAuthenticationService authService, IPersistenceService persistenceService)
        {
            InitializeComponent();

            // UIA Test-Stabilität: AutomationIds explizit setzen
            AutomationProperties.SetAutomationId(UsernameTextBox, "UsernameTextBox");
            AutomationProperties.SetAutomationId(PasswordBox, "PasswordBox");
            AutomationProperties.SetAutomationId(LoginButton, "LoginButton");
            AutomationProperties.SetAutomationId(ErrorMessage, "ErrorMessage");

            _vm = new LoginWindowViewModel(
                authService,
                persistenceService,
                openMainWindow: () =>
                {
                    var mainWindow = new MainWindow(persistenceService, authService);
                    mainWindow.Show();
                },
                closeLoginWindow: Close
            );

            DataContext = _vm;

            UsernameTextBox.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter)
                    _vm.LoginCommand.Execute(null);
            };

            PasswordBox.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter)
                    _vm.LoginCommand.Execute(null);
            };

            PasswordBox.PasswordChanged += (_, _) =>
            {
                _vm.Password = PasswordBox.Password;
            };

            _vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(LoginWindowViewModel.Password))
                {
                    if (PasswordBox.Password != (_vm.Password ?? string.Empty))
                        PasswordBox.Password = _vm.Password ?? string.Empty;
                }
            };
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
    }
}