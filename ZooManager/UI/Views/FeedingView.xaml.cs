using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Models;
using ZooManager.Infrastructure.Persistence;
using ZooManager.Infrastructure.Configuration;

namespace ZooManager.UI.Views
{
    public partial class FeedingView : UserControl
    {
        private SqlitePersistenceService _db;
        private readonly IPersistenceService _persistenceService;
        private readonly IAuthenticationService _authService;

        public FeedingView(IPersistenceService persistenceService, IAuthenticationService authService)
        {
            InitializeComponent();

            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            for (int i = 0; i < 24; i++) EditFeedingHour.Items.Add(i.ToString("D2"));
            for (int i = 0; i < 60; i += 5) EditFeedingMinute.Items.Add(i.ToString("D2"));

            ConfigureForUserRole();
            LoadPlan();
        }

        private void ConfigureForUserRole()
        {
            var currentUser = _authService?.GetCurrentUser();
            if (currentUser?.Role == UserRole.Employee)
            {
                SaveFeedingTime.Visibility = Visibility.Collapsed;
                EditFeedingDate.IsEnabled = false;
                EditFeedingHour.IsEnabled = false;
                EditFeedingMinute.IsEnabled = false;
            }
        }

        private void LoadPlan()
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null) return;

            IEnumerable<Animal> animals;

            if (currentUser.Role == UserRole.Employee)
            {
                if (!currentUser.EmployeeId.HasValue)
                {
                    FeedingList.ItemsSource = Array.Empty<Animal>();
                    ZooMessageBox.Show("Ihr Benutzer ist keinem Mitarbeiterprofil zugeordnet (EmployeeId fehlt).", "Fütterungsplan");
                    return;
                }

                animals = _persistenceService.LoadAnimalsForEmployee(currentUser.EmployeeId.Value);
            }
            else
            {
                animals = _persistenceService.LoadAnimals();
            }

            FeedingList.ItemsSource = animals.OrderBy(a => a.NextFeedingTime).ToList();
        }


        private void FeedingList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FeedingList.SelectedItem is Animal animal)
            {
                PlaceholderText.Visibility = Visibility.Collapsed;
                FeedingEditorArea.Visibility = Visibility.Visible;
            
                SelectedAnimalTitle.Text = $"Fütterung: {animal.Name} ({animal.SpeciesName})";
                EditFeedingDate.SelectedDate = animal.NextFeedingTime;
                EditFeedingHour.SelectedItem = animal.NextFeedingTime.Hour.ToString("D2");
                EditFeedingMinute.SelectedItem = (animal.NextFeedingTime.Minute / 5 * 5).ToString("D2");
            }
        }

        private void SaveFeedingTime_Click(object sender, RoutedEventArgs e)
        {
            // Nur für Manager verfügbar
            if (!_authService.HasPermission("EditFeeding"))
            {
                ZooMessageBox.Show("Sie haben keine Berechtigung, Fütterungszeiten zu ändern.", "Zugriff verweigert");
                return;
            }

            if (FeedingList.SelectedItem is Animal animal)
            {
                DateTime date = EditFeedingDate.SelectedDate ?? DateTime.Now;
                int h = int.Parse(EditFeedingHour.SelectedItem.ToString());
                int m = int.Parse(EditFeedingMinute.SelectedItem.ToString());
            
                animal.NextFeedingTime = new DateTime(date.Year, date.Month, date.Day, h, m, 0);
                _db.SaveAnimals(new List<Animal> { animal });
            
                ZooMessageBox.Show("Fütterungszeit wurde manuell angepasst.", "Planer");
                LoadPlan();
            }
        }

        private void FeedingDone_Click(object sender, RoutedEventArgs e)
        {
            var user = _authService.GetCurrentUser();
            if (user == null)
            {
                ZooMessageBox.Show("Sie sind nicht (mehr) angemeldet. Bitte neu anmelden.", "Sitzung abgelaufen");
                return;
            }

            if (FeedingList.SelectedItem is not Animal animal)
            {
                ZooMessageBox.Show("Bitte zuerst ein Tier auswählen.", "Keine Auswahl");
                return;
            }

            if (!_authService.HasPermission("ConfirmFeeding"))
            {
                ZooMessageBox.Show("Sie haben keine Berechtigung für diese Aktion.", "Zugriff verweigert");
                return;
            }

            animal.NextFeedingTime = animal.NextFeedingTime.AddDays(1);

            _persistenceService.SaveAnimals(new List<Animal> { animal });

            var feedingEvent = new AnimalEvent
            {
                Date = DateTime.Now,
                Type = "Fütterung",
                Description = $"Gefüttert von {user.Username}"
            };
            _persistenceService.AddAnimalEvent(animal.Id, feedingEvent);

            ZooMessageBox.Show($"{animal.Name} gefüttert. Nächster Termin: {animal.NextFeedingTime:ddd, dd.MM. HH:mm} Uhr", "Erfolg");
            LoadPlan();
        }
    }
}