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
        
        private AnimalEvent? _editingEvent;

        public AnimalsView(IPersistenceService persistenceService = null, IAuthenticationService authService = null)
        {
            InitializeComponent();
            _persistenceService = persistenceService;
            _authService = authService;

            FeedingHourSelector.Items.Clear();
            FeedingMinuteSelector.Items.Clear();

            for (int i = 0; i < 24; i++)
                FeedingHourSelector.Items.Add(i.ToString("D2"));

            for (int i = 0; i < 60; i += 5)
                FeedingMinuteSelector.Items.Add(i.ToString("D2"));

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

            NewAnimalFeedingDate.SelectedDate = DateTime.Today;
            FeedingHourSelector.SelectedItem = DateTime.Now.Hour.ToString("D2");
            FeedingMinuteSelector.SelectedItem = ((DateTime.Now.Minute / 5) * 5).ToString("D2");
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

            var newEv = new AnimalEvent
            {
                Date = NewEventDate.SelectedDate.Value,
                Type = NewEventType.Text,
                Description = NewEventDesc.Text
            };
            
            if (_editingEvent != null)
            {
                _persistenceService.UpdateAnimalEvent(selectedAnimal.Id, _editingEvent, newEv);
                
                selectedAnimal.Events.RemoveAll(x => IsSameEvent(x, _editingEvent));

                selectedAnimal.Events.Insert(0, newEv);

                RefreshEventsList(selectedAnimal);
                ExitEditMode();

                ZooMessageBox.Show("Ereignis wurde aktualisiert.", "Erfolg");
                return;
            }
            
            _persistenceService.AddAnimalEvent(selectedAnimal.Id, newEv);

            selectedAnimal.Events.Insert(0, newEv);
            RefreshEventsList(selectedAnimal);

            NewEventType.Text = "";
            NewEventDesc.Text = "";
            NewEventDate.SelectedDate = DateTime.Now;

            ZooMessageBox.Show("Ereignis wurde in der digitalen Akte gespeichert.", "Erfolg");
        }

        private void EditEvent_Click(object sender, RoutedEventArgs e)
        {
            if (!_authService.HasPermission("AddAnimalEvent"))
            {
                ZooMessageBox.Show("Sie haben keine Berechtigung, Ereignisse zu bearbeiten.", "Zugriff verweigert");
                return;
            }

            if (sender is not Button btn || btn.Tag is not AnimalEvent ev)
                return;

            _editingEvent = ev;

            EventEditorTitle.Text = "Ereignis bearbeiten";
            SaveEventBtn.Content = "Änderungen speichern";
            CancelEditEventBtn.Visibility = Visibility.Visible;

            NewEventDate.SelectedDate = ev.Date;
            NewEventType.Text = ev.Type;
            NewEventDesc.Text = ev.Description;
        }
        
        private void CancelEditEvent_Click(object sender, RoutedEventArgs e)
        {
            ExitEditMode();
        }

        private void DeleteEvent_Click(object sender, RoutedEventArgs e)
        {
            if (!_authService.HasPermission("AddAnimalEvent"))
            {
                ZooMessageBox.Show("Sie haben keine Berechtigung, Ereignisse zu löschen.", "Zugriff verweigert");
                return;
            }

            if (AnimalsList.SelectedItem is not Animal selectedAnimal)
                return;

            if (sender is not Button btn || btn.Tag is not AnimalEvent ev)
                return;

            _persistenceService.DeleteAnimalEvent(selectedAnimal.Id, ev);

            if (_editingEvent == ev)
                ExitEditMode();

            selectedAnimal.Events.Remove(ev);
            RefreshEventsList(selectedAnimal);

            ZooMessageBox.Show("Ereignis wurde gelöscht.", "Gelöscht");
        }

        private static bool IsSameEvent(AnimalEvent a, AnimalEvent b)
        {
            return a.Date == b.Date
                   && (a.Type ?? string.Empty) == (b.Type ?? string.Empty)
                   && (a.Description ?? string.Empty) == (b.Description ?? string.Empty);
        }

        private void RefreshEventsList(Animal animal)
        {
            EventsList.ItemsSource = null;
            EventsList.ItemsSource = animal.Events.OrderByDescending(x => x.Date).ToList();
        }

        private void ExitEditMode()
        {
            _editingEvent = null;

            if (EventEditorTitle != null) EventEditorTitle.Text = "Neues Ereignis erfassen";
            if (SaveEventBtn != null) SaveEventBtn.Content = "+ In Akte speichern";
            if (CancelEditEventBtn != null) CancelEditEventBtn.Visibility = Visibility.Collapsed;

            if (NewEventType != null) NewEventType.Text = "";
            if (NewEventDesc != null) NewEventDesc.Text = "";
            if (NewEventDate != null) NewEventDate.SelectedDate = DateTime.Now;
        }
    }
}