using System;
using System.Collections.ObjectModel;
using System.Linq;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Models;

namespace ZooManager.UI.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly IPersistenceService _persistenceService;
        private readonly IAuthenticationService _authService;
        private readonly Action _openFeedingPlan;
        private readonly Action _openEvents;

        private int _totalAnimals;
        private int _totalEnclosures;
        private int _totalEmployees;
        private bool _hasEvents;

        public int TotalAnimals
        {
            get => _totalAnimals;
            private set { _totalAnimals = value; OnPropertyChanged(); }
        }

        public int TotalEnclosures
        {
            get => _totalEnclosures;
            private set { _totalEnclosures = value; OnPropertyChanged(); }
        }

        public int TotalEmployees
        {
            get => _totalEmployees;
            private set { _totalEmployees = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Animal> FeedingPreview { get; } = new();
        public ObservableCollection<ZooEvent> EventsPreview { get; } = new();

        public bool HasEvents
        {
            get => _hasEvents;
            private set
            {
                _hasEvents = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowEventsCard));
                OnPropertyChanged(nameof(ShowNoEventsText));
            }
        }

        public bool ShowEventsCard => HasEvents;
        public bool ShowNoEventsText => !HasEvents;

        public RelayCommand OpenFeedingPlanCommand { get; }
        public RelayCommand OpenEventsCommand { get; }
        public RelayCommand RefreshCommand { get; }

        public DashboardViewModel(
            IPersistenceService persistenceService,
            IAuthenticationService authService,
            Action openFeedingPlan,
            Action openEvents)
        {
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _openFeedingPlan = openFeedingPlan ?? throw new ArgumentNullException(nameof(openFeedingPlan));
            _openEvents = openEvents ?? throw new ArgumentNullException(nameof(openEvents));

            OpenFeedingPlanCommand = new RelayCommand(_ => _openFeedingPlan());
            OpenEventsCommand = new RelayCommand(_ => _openEvents());
            RefreshCommand = new RelayCommand(_ => LoadDashboardStats());

            LoadDashboardStats();
        }

        private void LoadDashboardStats()
        {
            var user = _authService.GetCurrentUser();
            if (user == null)
                return;

            var allAnimals = _persistenceService.LoadAnimals().ToList();
            var allEnclosures = _persistenceService.LoadEnclosures().ToList();
            var allEmployees = _persistenceService.LoadEmployees().ToList();

            TotalAnimals = allAnimals.Count;
            TotalEnclosures = allEnclosures.Count;
            TotalEmployees = allEmployees.Count;

            var feedingAnimals = allAnimals;
            if (user.Role == UserRole.Employee && user.EmployeeId.HasValue)
                feedingAnimals = _persistenceService.LoadAnimalsForEmployee(user.EmployeeId.Value).ToList();

            FeedingPreview.Clear();
            foreach (var a in feedingAnimals
                         .OrderBy(a => a.NextFeedingTime)
                         .Take(3))
            {
                FeedingPreview.Add(a);
            }

            var allEvents = _persistenceService.LoadEvents()
                .OrderBy(e => e.Start)
                .ToList();

            var upcoming = allEvents
                .Where(e => e.Start >= DateTime.Now)
                .Take(3)
                .ToList();

            var toShow = upcoming.Count > 0
                ? upcoming
                : allEvents.TakeLast(3).ToList();

            EventsPreview.Clear();
            foreach (var ev in toShow)
                EventsPreview.Add(ev);

            HasEvents = EventsPreview.Count > 0;
        }
    }
}