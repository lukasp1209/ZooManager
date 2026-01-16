using System.Windows.Controls;
using ZooManager.Infrastructure.Persistence;
using ZooManager.Infrastructure.Configuration;
using System.Linq;
using System.Windows;
using ZooManager.Core.Models;

namespace ZooManager.UI.Views
{
    public partial class AnimalsView : UserControl
    {
        public AnimalsView()
        {
            InitializeComponent();
            
            for (int i = 0; i < 24; i++) FeedingHourSelector.Items.Add(i.ToString("D2"));
            for (int i = 0; i < 60; i += 5) FeedingMinuteSelector.Items.Add(i.ToString("D2"));
            
            LoadData();
        }

        private void LoadData()
        {
            var db = new MySqlPersistenceService(DatabaseConfig.GetConnectionString());
            var animals = db.LoadAnimals().ToList();
            AnimalsList.ItemsSource = animals;
            
            SpeciesSelector.ItemsSource = db.LoadSpecies().ToList();
            EnclosureSelector.ItemsSource = db.LoadEnclosures().ToList();

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

            var db = new MySqlPersistenceService(DatabaseConfig.GetConnectionString());
            var newAnimal = new Core.Models.Animal
            {
                Name = NewAnimalName.Text,
                SpeciesId = ((Core.Models.Species)SpeciesSelector.SelectedItem).Id,
                EnclosureId = EnclosureSelector.SelectedItem != null ? ((Core.Models.Enclosure)EnclosureSelector.SelectedItem).Id : 0,
                NextFeedingTime = combinedFeedingTime
            };
            
            db.SaveAnimals(new List<Core.Models.Animal> { newAnimal });
            
            ZooMessageBox.Show($"{newAnimal.Name} wurde erfolgreich angelegt!", "Erfolg");
            CancelEditor_Click(null, null);
            LoadData();
        }

        private void DeleteAnimal_Click(object sender, RoutedEventArgs e)
        {
            if (AnimalsList.SelectedItem is Core.Models.Animal selected)
            {
                var db = new MySqlPersistenceService(DatabaseConfig.GetConnectionString());
                
                using (var conn = new MySql.Data.MySqlClient.MySqlConnection(DatabaseConfig.GetConnectionString()))
                {
                    conn.Open();
                    var cmd = new MySql.Data.MySqlClient.MySqlCommand("DELETE FROM Animals WHERE Id = @id", conn);
                    cmd.Parameters.AddWithValue("@id", selected.Id);
                    cmd.ExecuteNonQuery();
                }

                ZooMessageBox.Show($"{selected.Name} wurde aus dem Bestand entfernt.", "Gelöscht");
                LoadData();
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

                var db = new MySqlPersistenceService(DatabaseConfig.GetConnectionString());
                db.AddAnimalEvent(selectedAnimal.Id, newEvent);
                
                selectedAnimal.Events.Add(newEvent);
                var sortedEvents = selectedAnimal.Events.OrderByDescending(x => x.Date).ToList();
                selectedAnimal.Events = sortedEvents;
                EventsList.ItemsSource = sortedEvents;

                NewEventType.Text = "";
                NewEventDesc.Text = "";
                NewEventDate.SelectedDate = System.DateTime.Now;
                
                ZooMessageBox.Show("Ereignis wurde in der digitalen Akte gespeichert.", "Erfolg");
            }
        }
    }
}