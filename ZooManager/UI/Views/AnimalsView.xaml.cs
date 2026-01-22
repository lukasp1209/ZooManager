using System.Collections.Generic;
using System.Windows.Controls;
using ZooManager.Infrastructure.Persistence;
using ZooManager.Infrastructure.Configuration;
using System.Linq;
using System.Windows;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Models;

namespace ZooManager.UI.Views
{
    public partial class AnimalsView : UserControl
    {
        private readonly IPersistenceService? _persistenceService;
        private readonly IAuthenticationService? _authService;

        public AnimalsView(IPersistenceService persistenceService = null, IAuthenticationService authService = null)
        {
            InitializeComponent();
            _persistenceService = persistenceService;
            _authService = authService;

            LoadData();
            ApplyPermissions();
        }
        
        private void ApplyPermissions()
        {
            var role = _authService.GetCurrentUser()?.Role;

            if (role == UserRole.Employee)
            {
                if (AddAnimalBtn != null) AddAnimalBtn.Visibility = Visibility.Collapsed;
                if (DeleteAnimalBtn != null) DeleteAnimalBtn.Visibility = Visibility.Collapsed;

                EditorArea.Visibility = Visibility.Collapsed;
                DetailsArea.Visibility = Visibility.Visible;
            }
        }

        private void LoadData()
        {
            AnimalsList.ItemsSource = _persistenceService.LoadAnimals().ToList();

            if (_authService.GetCurrentUser()?.Role != UserRole.Employee)
            {
                SpeciesSelector.ItemsSource = _persistenceService.LoadSpecies().ToList();
                EnclosureSelector.ItemsSource = _persistenceService.LoadEnclosures().ToList();
            }

            NewEventDate.SelectedDate = DateTime.Now;
        }

        private void AnimalsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AnimalsList.SelectedItem is Animal selectedAnimal)
            {
                SelectedAnimalName.Text = selectedAnimal.Name;
                SelectedAnimalSpecies.Text = selectedAnimal.SpeciesName;
                SelectedAnimalNameDetail.Text = selectedAnimal.Name;
                SelectedAnimalEnclosure.Text = selectedAnimal.EnclosureName;
                SelectedAnimalFeeding.Text = selectedAnimal.NextFeedingTime.ToString("g");

                EventsList.ItemsSource = selectedAnimal.Events
                    .OrderByDescending(x => x.Date)
                    .ToList();
            }
            else
            {
                EventsList.ItemsSource = null;
            }
        }

        private void AddAnimal_Click(object sender, RoutedEventArgs e)
        {
            DetailsArea.Visibility = Visibility.Collapsed;
            EditorArea.Visibility = Visibility.Visible;
            NewAnimalName.Text = "";
        }

        private void CancelEditor_Click(object sender, RoutedEventArgs e)
        {
            EditorArea.Visibility = Visibility.Collapsed;
            DetailsArea.Visibility = Visibility.Visible;
        }

        private void SaveNewAnimal_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewAnimalName.Text) || SpeciesSelector.SelectedItem == null)
            {
                ZooMessageBox.Show("Bitte Name und Tierart angeben.", "Eingabefehler");
                return;
            }
            
            System.DateTime date = NewAnimalFeedingDate.SelectedDate ?? System.DateTime.Now;
            int hour = int.Parse(FeedingHourSelector.SelectedItem?.ToString() ?? "0");
            int minute = int.Parse(FeedingMinuteSelector.SelectedItem?.ToString() ?? "0");
            System.DateTime combinedFeedingTime = new System.DateTime(date.Year, date.Month, date.Day, hour, minute, 0);

            var existingAnimals = _persistenceService.LoadAnimals().ToList();
            int newId = existingAnimals.Any() ? existingAnimals.Max(a => a.Id) + 1 : 1;

            var newAnimal = new Core.Models.Animal
            {
                Id = newId,
                Name = NewAnimalName.Text,
                SpeciesId = ((Core.Models.Species)SpeciesSelector.SelectedItem).Id,
                EnclosureId = EnclosureSelector.SelectedItem != null ? ((Core.Models.Enclosure)EnclosureSelector.SelectedItem).Id : null,
                NextFeedingTime = combinedFeedingTime
            };
            
            _persistenceService.SaveAnimals(new List<Core.Models.Animal> { newAnimal });
            
            ZooMessageBox.Show($"{newAnimal.Name} wurde erfolgreich angelegt!", "Erfolg");
            CancelEditor_Click(null, null);
            LoadData();
        }

        private void DeleteAnimal_Click(object sender, RoutedEventArgs e)
        {
            if (_authService.GetCurrentUser()?.Role == UserRole.Employee)
            {
                ZooMessageBox.Show("Sie haben keine Berechtigung, Tiere zu löschen.", "Zugriff verweigert");
                return;
            }

            if (_persistenceService == null)
            {
                ZooMessageBox.Show("Datenbank-Service ist nicht initialisiert.", "Fehler");
                return;
            }

            if (AnimalsList.SelectedItem is not Animal selected)
                return;

            _persistenceService.DeleteAnimal(selected.Id);

            ZooMessageBox.Show($"{selected.Name} wurde aus dem Bestand entfernt.", "Gelöscht");
            LoadData();
        }

        private void AddEvent_Click(object sender, RoutedEventArgs e)
        {
            if (!_authService.HasPermission("AddAnimalEvent"))
            {
                ZooMessageBox.Show("Sie haben keine Berechtigung, Ereignisse hinzuzufügen.", "Zugriff verweigert");
                return;
            }

            if (AnimalsList.SelectedItem is not Animal selectedAnimal)
                return;

            if (string.IsNullOrWhiteSpace(NewEventType.Text) ||
                string.IsNullOrWhiteSpace(NewEventDesc.Text) ||
                NewEventDate.SelectedDate == null)
            {
                ZooMessageBox.Show("Bitte Datum, Typ und Beschreibung vollständig ausfüllen.", "Eingabe unvollständig");
                return;
            }
            
            var ev = new AnimalEvent
            {
                Date = NewEventDate.SelectedDate.Value,
                Type = NewEventType.Text,
                Description = NewEventDesc.Text
            };
            
            _persistenceService.AddAnimalEvent(selectedAnimal.Id, ev);
            
            selectedAnimal.Events.Insert(0, ev);
            EventsList.ItemsSource = null;
            EventsList.ItemsSource = selectedAnimal.Events;

            NewEventType.Text = "";
            NewEventDesc.Text = "";
            NewEventDate.SelectedDate = DateTime.Now;

            ZooMessageBox.Show("Ereignis wurde in der digitalen Akte gespeichert.", "Erfolg");
        }
    }
}