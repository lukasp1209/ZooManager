using System;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Models;
using ZooManager.UI.Views;

namespace ZooManager.UI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IPersistenceService _persistenceService;
        private readonly IAuthenticationService _authService;
        private readonly Action _closeMainWindow;
        private readonly Action _openLoginWindow;
        private readonly Func<bool> _confirmLogout;

        private object? _currentView;

        public object? CurrentView
        {
            get => _currentView;
            private set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public string UsernameText { get; }
        public string UserRoleText { get; }

        public string WindowTitle { get; }

        public bool ShowDashboard { get; }
        public bool ShowAnimals { get; }
        public bool ShowSpecies { get; }
        public bool ShowEnclosures { get; }
        public bool ShowEmployees { get; }
        public bool ShowFeedingPlan { get; }
        public bool ShowEvents { get; }
        public bool ShowReports { get; }

        public string DashboardButtonText { get; }

        public RelayCommand ShowDashboardCommand { get; }
        public RelayCommand ShowAnimalsCommand { get; }
        public RelayCommand ShowFeedingPlanCommand { get; }
        public RelayCommand ShowSpeciesCommand { get; }
        public RelayCommand ShowEnclosuresCommand { get; }
        public RelayCommand ShowEmployeesCommand { get; }
        public RelayCommand ShowEventsCommand { get; }
        public RelayCommand ShowReportsCommand { get; }

        public RelayCommand LogoutCommand { get; }

        public MainWindowViewModel(
            IPersistenceService persistenceService,
            IAuthenticationService authService,
            Action openLoginWindow,
            Action closeMainWindow,
            Func<bool> confirmLogout)
        {
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _openLoginWindow = openLoginWindow ?? throw new ArgumentNullException(nameof(openLoginWindow));
            _closeMainWindow = closeMainWindow ?? throw new ArgumentNullException(nameof(closeMainWindow));
            _confirmLogout = confirmLogout ?? throw new ArgumentNullException(nameof(confirmLogout));

            var user = _authService.GetCurrentUser();
            if (user == null)
            {
                UsernameText = "Nicht angemeldet";
                UserRoleText = "";
                WindowTitle = "ZooManager";
            }
            else
            {
                UsernameText = user.Username;

                UserRoleText = user.Role switch
                {
                    UserRole.Admin => "Administrator",
                    UserRole.ZooManager => "Zoo Manager",
                    UserRole.Employee => "Mitarbeiter",
                    _ => "Unbekannt"
                };

                WindowTitle = user.Role == UserRole.ZooManager || user.Role == UserRole.Admin
                    ? "ZooManager - Verwaltung"
                    : $"ZooManager - Mitarbeiterbereich ({user.Username})";
            }

            var isEmployee = user?.Role == UserRole.Employee;

            // Sichtbarkeit pro Rolle (wie vorher)
            ShowDashboard = true;
            ShowAnimals = true;
            ShowFeedingPlan = true;
            ShowEvents = true;

            ShowSpecies = !isEmployee;
            ShowEnclosures = !isEmployee;
            ShowEmployees = !isEmployee;
            ShowReports = !isEmployee;

            DashboardButtonText = isEmployee ? "Übersicht" : "Dashboard";

            ShowDashboardCommand = new RelayCommand(_ => Navigate("Dashboard"));
            ShowAnimalsCommand = new RelayCommand(_ => Navigate("Animals"));
            ShowFeedingPlanCommand = new RelayCommand(_ => Navigate("FeedingPlan"));
            ShowSpeciesCommand = new RelayCommand(_ => Navigate("Species"));
            ShowEnclosuresCommand = new RelayCommand(_ => Navigate("Enclosures"));
            ShowEmployeesCommand = new RelayCommand(_ => Navigate("Employees"));
            ShowEventsCommand = new RelayCommand(_ => Navigate("Events"));
            ShowReportsCommand = new RelayCommand(_ => Navigate("Reports"));

            LogoutCommand = new RelayCommand(_ => Logout());

            // Startansicht
            Navigate("Dashboard");
        }

        private void Navigate(string target)
        {
            // Dashboard immer erlaubt
            if (target != "Dashboard" && !_authService.HasPermission($"View{target}"))
            {
                ZooMessageBox.Show("Sie haben keine Berechtigung für diesen Bereich.", "Zugriff verweigert");
                return;
            }

            CurrentView = target switch
            {
                "Dashboard" => new DashboardView(_persistenceService, _authService),
                "Animals" => new AnimalsView(_persistenceService, _authService),
                "FeedingPlan" => new FeedingView(_persistenceService, _authService),
                "Species" => new SpeciesView(_persistenceService),
                "Enclosures" => new EnclosuresView(_persistenceService),
                "Employees" => new EmployeesView(_persistenceService, _authService),
                "Events" => new EventsView(_persistenceService, _authService),
                "Reports" => new ReportsView(_persistenceService),
                _ => CurrentView
            };
        }

        private void Logout()
        {
            if (!_confirmLogout())
                return;

            _authService.Logout();
            _openLoginWindow();
            _closeMainWindow();
        }
    }
}