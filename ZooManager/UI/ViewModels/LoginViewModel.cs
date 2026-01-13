namespace ZooManager.UI.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private string? _username;
        private string? _errorMessage;
        private bool _hasError;

        public string? Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public bool HasError
        {
            get => _hasError;
            set { _hasError = value; OnPropertyChanged(); }
        }

        public RelayCommand LoginCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(Login);
        }

        private void Login(object? parameter)
        {
            string? password = parameter as string;

            HasError = false;

            if (string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(password))
            {
                ShowError("Username and password are required.");
                return;
            }

            // ❗ DEMO AUTH (replace with DB / API)
            if (Username == "admin" && password == "admin")
            {
                // TODO: Open MainWindow
            }
            else
            {
                ShowError("Invalid username or password.");
            }
        }

        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }
    }
}