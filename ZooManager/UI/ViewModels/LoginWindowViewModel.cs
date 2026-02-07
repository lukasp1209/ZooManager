using System;
using System.Windows;
using ZooManager.Core.Interfaces;

namespace ZooManager.UI.ViewModels
{
    public class LoginWindowViewModel : ViewModelBase
    {
        private readonly IAuthenticationService _authService;
        private readonly IPersistenceService _persistenceService;
        private readonly Action _openMainWindow;
        private readonly Action _closeLoginWindow;

        private string? _username;
        private string? _password;
        private string? _errorMessage;
        private bool _isErrorVisible;

        public string? Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        // Wird über Code-Behind gesetzt (PasswordBox ist nicht bindable)
        public string? Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set { _errorMessage = value; OnPropertyChanged(); }
        }

        public bool IsErrorVisible
        {
            get => _isErrorVisible;
            private set { _isErrorVisible = value; OnPropertyChanged(); }
        }

        public RelayCommand LoginCommand { get; }

        public LoginWindowViewModel(
            IAuthenticationService authService,
            IPersistenceService persistenceService,
            Action openMainWindow,
            Action closeLoginWindow)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            _openMainWindow = openMainWindow ?? throw new ArgumentNullException(nameof(openMainWindow));
            _closeLoginWindow = closeLoginWindow ?? throw new ArgumentNullException(nameof(closeLoginWindow));

            LoginCommand = new RelayCommand(_ => Login());
        }

        private void Login()
        {
            HideError();

            var username = (Username ?? string.Empty).Trim();
            var password = Password ?? string.Empty;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Bitte geben Sie Benutzername und Passwort ein.");
                return;
            }

            var user = _authService.AuthenticateUser(username, password);
            if (user != null)
            {
                // UI-Aktion über Callback, damit ViewModel nicht direkt Window new't.
                _openMainWindow();
                _closeLoginWindow();
                return;
            }

            ShowError("Ungültige Anmeldedaten. Bitte versuchen Sie es erneut.");

            // optional: Passwort nach Fehler zurücksetzen
            Password = string.Empty;
        }

        private void ShowError(string message)
        {
            ErrorMessage = message;
            IsErrorVisible = true;
        }

        private void HideError()
        {
            IsErrorVisible = false;
            ErrorMessage = null;
        }
    }
}