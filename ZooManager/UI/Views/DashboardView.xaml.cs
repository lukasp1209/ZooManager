using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Models;

namespace ZooManager.UI.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly IPersistenceService _persistenceService;
        private readonly IAuthenticationService _authService;

        public DashboardView(IPersistenceService persistenceService, IAuthenticationService authService)
        {
            InitializeComponent();
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            LoadDashboardStats();
        }

        private void LoadDashboardStats()
        {
            var user = _authService.GetCurrentUser();
            if (user == null) return;

            var allAnimals = _persistenceService.LoadAnimals().ToList();
            var allEnclosures = _persistenceService.LoadEnclosures().ToList();
            var allEmployees = _persistenceService.LoadEmployees().ToList();

            TotalAnimalsText.Text = allAnimals.Count.ToString();
            TotalEnclosuresText.Text = allEnclosures.Count.ToString();
            TotalEmployeesText.Text = allEmployees.Count.ToString();

            var feedingAnimals = allAnimals;
            if (user.Role == UserRole.Employee && user.EmployeeId.HasValue)
                feedingAnimals = _persistenceService.LoadAnimalsForEmployee(user.EmployeeId.Value).ToList();

            FeedingPreviewList.ItemsSource = feedingAnimals
                .OrderBy(a => a.NextFeedingTime)
                .Take(3)
                .ToList();

            var allEvents = _persistenceService.LoadEvents()
                .OrderBy(e => e.Start)
                .ToList();

            var upcoming = allEvents.Where(e => e.Start >= DateTime.Now).Take(3).ToList();

            var toShow = upcoming.Count > 0
                ? upcoming
                : allEvents.TakeLast(3).ToList();

            EventsPreviewList.ItemsSource = toShow;

            if (EventsPreviewCard != null)
                EventsPreviewCard.Visibility = toShow.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            if (NoEventsText != null)
                NoEventsText.Visibility = toShow.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OpenFeedingPlan_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.MainContentPresenter.Content = new FeedingView(_persistenceService, _authService);
        }

        private void OpenEvents_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.MainContentPresenter.Content = new EventsView(_persistenceService, _authService);
        }
    }
}
