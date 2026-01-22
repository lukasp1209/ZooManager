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
        private SqlitePersistenceService _db;

        public AnimalsView(IPersistenceService persistenceService = null)
        {
            InitializeComponent();
            _db = persistenceService as SqlitePersistenceService ?? 
                  new SqlitePersistenceService(DatabaseConfig.GetConnectionString());
            
            for (int i = 0; i < 24; i++) FeedingHourSelector.Items.Add(i.ToString("D2"));
            for (int i = 0; i < 60; i += 5) FeedingMinuteSelector.Items.Add(i.ToString("D2"));
            
            LoadData();
        }

        private void LoadData()
        {
            var animals = _db.LoadAnimals().ToList();
            AnimalsList.ItemsSource = animals;
            
            SpeciesSelector.ItemsSource = _db.LoadSpecies().ToList();
            EnclosureSelector.ItemsSource = _db.LoadEnclosures().ToList();

            NewEventDate.SelectedDate = System.DateTime.Now;
            NewAnimalFeedingDate.SelectedDate = System.DateTime.Now;
            FeedingHourSelector.SelectedIndex = 12;
            FeedingMinuteSelector.SelectedIndex = 0;
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

            var existingAnimals = _db.LoadAnimals().ToList();
            int newId = existingAnimals.Any() ? existingAnimals.Max(a => a.Id) + 1 : 1;

            var newAnimal = new Core.Models.Animal
            {
                Id = newId,
                Name = NewAnimalName.Text,
                SpeciesId = ((Core.Models.Species)SpeciesSelector.SelectedItem).Id,
                EnclosureId = EnclosureSelector.SelectedItem != null ? ((Core.Models.Enclosure)EnclosureSelector.SelectedItem).Id : null,
                NextFeedingTime = combinedFeedingTime
            };
            
            _db.SaveAnimals(new List<Core.Models.Animal> { newAnimal });
            
            ZooMessageBox.Show($"{newAnimal.Name} wurde erfolgreich angelegt!", "Erfolg");
            CancelEditor_Click(null, null);
            LoadData();
        }

        private void DeleteAnimal_Click(object sender, RoutedEventArgs e)
        {
            if (AnimalsList.SelectedItem is Core.Models.Animal selected)
            {
                try
                {
                    _db.DeleteAnimal(selected.Id);
                    ZooMessageBox.Show($"{selected.Name} wurde aus dem Bestand entfernt.", "Gelöscht");
                    LoadData();
                }
                catch (System.Exception ex)
                {
                    ZooMessageBox.Show($"Fehler beim Löschen: {ex.Message}", "Datenbankfehler");
                }
            }
            else
            {
                ZooMessageBox.Show("Bitte wählen Sie zuerst ein Tier aus der Liste aus.", "Hinweis");
            }
        }

        private void AddEvent_Click(object sender, RoutedEventArgs e)
        {
            if (AnimalsList.SelectedItem is Core.Models.Animal selectedAnimal)
            {
                if (string.IsNullOrWhiteSpace(NewEventType.Text) || 
                    string.IsNullOrWhiteSpace(NewEventDesc.Text) || 
                    NewEventDate.SelectedDate == null)
                {
                    ZooMessageBox.Show("Bitte Datum, Typ und Beschreibung vollständig ausfüllen.", "Eingabe unvollständig");
                    return;
                }

                var newEvent = new Core.Models.AnimalEvent
                {
                    Date = NewEventDate.SelectedDate.Value,
                    Type = NewEventType.Text,
                    Description = NewEventDesc.Text
                };

                _db.AddAnimalEvent(selectedAnimal.Id, newEvent);
                
                // Liste neu laden um konsistente Daten zu haben
                LoadData();

                NewEventType.Text = "";
                NewEventDesc.Text = "";
                NewEventDate.SelectedDate = System.DateTime.Now;
                
                ZooMessageBox.Show("Ereignis wurde in der digitalen Akte gespeichert.", "Erfolg");
            }
        }
    }
}