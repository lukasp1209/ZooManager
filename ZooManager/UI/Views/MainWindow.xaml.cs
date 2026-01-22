using System;
using System.Windows;
using System.Windows.Controls;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Models;

namespace ZooManager.UI.Views
{
    public partial class MainWindow : Window
    {
        private readonly IPersistenceService _persistenceService;
        private readonly IAuthenticationService _authService;

        public MainWindow(IPersistenceService persistenceService, IAuthenticationService authService)
        {
            InitializeComponent();
            _persistenceService = persistenceService;
            _authService = authService;

            DisplayUserInfo();
            SetupUserInterface();

            MainContentPresenter.Content = new DashboardView(_persistenceService, authService);
        }

        private void DisplayUserInfo()
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser != null)
            {
                UsernameText.Text = currentUser.Username;

                UserRoleText.Text = currentUser.Role switch
                {
                    UserRole.Admin => "Administrator",
                    UserRole.ZooManager => "Zoo Manager",
                    UserRole.Employee => "Mitarbeiter",
                    _ => "Unbekannt"
                };
            }
            else
            {
                UsernameText.Text = "Nicht angemeldet";
                UserRoleText.Text = "";
            }
        }

        private void SetupUserInterface()
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
            {
                Close();
                return;
            }

            Title = currentUser.Role == UserRole.ZooManager || currentUser.Role == UserRole.Admin
                ? "ZooManager - Verwaltung"
                : $"ZooManager - Mitarbeiterbereich ({currentUser.Username})";

            ConfigureNavigationForRole(currentUser.Role);
        }

        private void ConfigureNavigationForRole(UserRole role)
        {
            if (role == UserRole.Employee)
            {
                HideNavigationButton("Species");
                HideNavigationButton("Enclosures");
                HideNavigationButton("Employees");
                HideNavigationButton("Reports");

                if (DashboardButton != null)
                    DashboardButton.Content = "Übersicht";
            }
        }

        private void HideNavigationButton(string buttonName)
        {
            var button = FindName(buttonName + "Button") as Button;
            if (button != null)
                button.Visibility = Visibility.Collapsed;
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag == null)
                return;

            var target = button.Tag.ToString();

            // Dashboard immer erlaubt
            if (target != "Dashboard" && !_authService.HasPermission($"View{target}"))
            {
                ZooMessageBox.Show("Sie haben keine Berechtigung für diesen Bereich.", "Zugriff verweigert");
                return;
            }

            switch (target)
            {
                case "Dashboard":
                    MainContentPresenter.Content = new DashboardView(_persistenceService, _authService);
                    break;

                case "Animals":
                    MainContentPresenter.Content = new AnimalsView(_persistenceService, _authService);
                    break;

                case "FeedingPlan":
                    MainContentPresenter.Content = new FeedingView(_persistenceService, _authService);
                    break;

                case "Species":
                    MainContentPresenter.Content = new SpeciesView(_persistenceService);
                    break;

                case "Enclosures":
                    MainContentPresenter.Content = new EnclosuresView(_persistenceService);
                    break;

                case "Employees":
                    MainContentPresenter.Content = new EmployeesView(_persistenceService, _authService);
                    break;

                case "Events":
                    MainContentPresenter.Content = new EventsView(_persistenceService, _authService);
                    break;

                case "Reports":
                    MainContentPresenter.Content = new ReportsView(_persistenceService);
                    break;
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var logoutDialog = new LogoutDialog { Owner = this };
            logoutDialog.ShowDialog();

            if (!logoutDialog.Confirmed)
                return;

            _authService.Logout();

            var loginWindow = new LoginWindow(_authService, _persistenceService);
            loginWindow.Show();

            Close();
        }
    }
}