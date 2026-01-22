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
        private readonly IAuthenticationService _authService;

        public FeedingView(IPersistenceService persistenceService = null, IAuthenticationService authService = null)
        {
            InitializeComponent();
            _db = persistenceService as SqlitePersistenceService ?? 
                  new SqlitePersistenceService(DatabaseConfig.GetConnectionString());
            _authService = authService;
        
            for (int i = 0; i < 24; i++) EditFeedingHour.Items.Add(i.ToString("D2"));
            for (int i = 0; i < 60; i += 5) EditFeedingMinute.Items.Add(i.ToString("D2"));
        
            LoadPlan();
            ConfigureForUserRole();
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
                
                // Hilfsmeldung für Mitarbeiter
                var helpText = new TextBlock
                {
                    Text = "Als Mitarbeiter können Sie nur die Fütterung bestätigen, aber keine Zeiten ändern.",
                    Foreground = System.Windows.Media.Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 10, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                };
                
                // Annahme: Es gibt ein StackPanel namens "FeedingEditorArea" 
                if (FeedingEditorArea.FindName("EditorStack") is StackPanel editorStack)
                {
                    editorStack.Children.Add(helpText);
                }
            }
        }

        private void LoadPlan()
        {
            var currentUser = _authService?.GetCurrentUser();
            IEnumerable<Animal> animals;
            
            if (currentUser?.Role == UserRole.Employee && currentUser.EmployeeId.HasValue)
            {
                // Mitarbeiter sehen nur Tiere, die sie füttern dürfen (basierend auf Qualifikationen)
                animals = _db.LoadAnimalsForEmployee(currentUser.EmployeeId.Value);
            }
            else
            {
                // Manager sehen alle Tiere
                animals = _db.LoadAnimals();
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
            if (!_authService.HasPermission("ConfirmFeeding"))
            {
                ZooMessageBox.Show("Sie haben keine Berechtigung für diese Aktion.", "Zugriff verweigert");
                return;
            }

            if (FeedingList.SelectedItem is Animal animal)
            {
                var currentUser = _authService.GetCurrentUser();
                
                // Bei Mitarbeitern prüfen, ob sie für diese Tierart qualifiziert sind
                if (currentUser?.Role == UserRole.Employee && currentUser.EmployeeId.HasValue)
                {
                    var qualifiedAnimals = _db.LoadAnimalsForEmployee(currentUser.EmployeeId.Value);
                    if (!qualifiedAnimals.Any(a => a.Id == animal.Id))
                    {
                        ZooMessageBox.Show("Sie sind nicht qualifiziert, dieses Tier zu füttern.", "Zugriff verweigert");
                        return;
                    }
                }
                
                animal.NextFeedingTime = animal.NextFeedingTime.AddDays(1);
            
                _db.SaveAnimals(new List<Animal> { animal });
                
                // Event für die Fütterung hinzufügen
                var feedingEvent = new AnimalEvent
                {
                    Date = DateTime.Now,
                    Type = "Fütterung",
                    Description = $"Gefüttert von {currentUser?.Username ?? "System"}"
                };
                _db.AddAnimalEvent(animal.Id, feedingEvent);
                
                ZooMessageBox.Show($"{animal.Name} gefüttert. Nächster Termin morgen um {animal.NextFeedingTime:HH:mm} Uhr.", "Erfolg");
                LoadPlan();
            }
        }
    }
}