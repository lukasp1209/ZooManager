using System;
using System.Windows;
using System.Windows.Controls;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Models;
using ZooManager.Infrastructure.Persistence;

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
                
                // Rolle anzeigen
                UserRoleText.Text = currentUser.Role switch
                {
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

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var logoutDialog = new LogoutDialog
            {
                Owner = this
            };
            logoutDialog.ShowDialog();
            
            if (logoutDialog.Confirmed)
            {
                _authService.Logout();
                
                var loginWindow = new LoginWindow(_authService, _persistenceService);
                loginWindow.Show();
                
                this.Close();
            }
        }

        private void SetupUserInterface()
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
            {
                this.Close();
                return;
            }

            // Titel anpassen basierend auf Benutzerrolle
            this.Title = currentUser.Role == UserRole.ZooManager 
                ? "ZooManager - Administratorbereich" 
                : $"ZooManager - Mitarbeiterbereich ({currentUser.Username})";

            // Navigation basierend auf Berechtigung anpassen
            ConfigureNavigationForRole(currentUser.Role);
        }

        private void ConfigureNavigationForRole(UserRole role)
        {
            if (role == UserRole.Employee)
            {
                // Mitarbeiter sehen nur bestimmte Bereiche
                HideNavigationButton("Species");
                HideNavigationButton("Enclosures"); 
                HideNavigationButton("Employees");
                HideNavigationButton("Reports");
                
                // Dashboard für Mitarbeiter einschränken
                var dashboardBtn = FindName("DashboardButton") as Button;
                if (dashboardBtn != null)
                    dashboardBtn.Content = "🏠 Übersicht";
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
            if (sender is Button button && button.Tag != null)
            {
                string target = button.Tag.ToString();
                
                if (!_authService.HasPermission($"View{target}") && target != "Dashboard")
                {
                    MessageBox.Show("Sie haben keine Berechtigung für diesen Bereich.", "Zugriff verweigert", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            
                switch (target)
                {
                    case "Dashboard":
                        MainContentPresenter.Content = new DashboardView(_persistenceService, _authService);
                        break;
                    case "FeedingPlan":
                        MainContentPresenter.Content = new FeedingView(_persistenceService, _authService);
                        break;
                    case "Animals":
                        MainContentPresenter.Content = new AnimalsView(_persistenceService, _authService);
                        break;
                    case "Species":
                        if (_authService.HasPermission("ViewSpecies"))
                            MainContentPresenter.Content = new SpeciesView(_persistenceService);
                        break;
                    case "Enclosures":
                        if (_authService.HasPermission("ViewEnclosures"))
                            MainContentPresenter.Content = new EnclosuresView(_persistenceService);
                        break;
                    case "Employees":
                        if (_authService.HasPermission("ViewEmployees"))
                            MainContentPresenter.Content = new EmployeesView(_persistenceService);
                        break;
                    case "Events":
                        MainContentPresenter.Content = new EventsView(_persistenceService, _authService);
                        break;
                    case "Reports":
                        if (_authService.HasPermission("ViewReports"))
                            MainContentPresenter.Content = new ReportsView(_persistenceService);
                        break;
                }
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Möchten Sie sich wirklich abmelden?", "Abmelden", 
                                       MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _authService.Logout();
                
                var loginWindow = new LoginWindow(_authService, _persistenceService);
                loginWindow.Show();
                this.Close();
            }
        }
    }
}