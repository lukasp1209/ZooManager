using System.Windows.Controls;
using ZooManager.Infrastructure.Persistence;
using ZooManager.Infrastructure.Configuration;
using System.Linq;
using System.Windows;

namespace ZooManager.UI.Views
{
    public partial class AnimalsView : UserControl
    {
        public AnimalsView()
        {
            InitializeComponent();
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
        }

        private void AnimalsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AnimalsList.SelectedItem is Core.Models.Animal selectedAnimal)
            {
                SelectedAnimalName.Text = selectedAnimal.Name;
                SelectedAnimalSpecies.Text = selectedAnimal.SpeciesName;
                SelectedAnimalNameDetail.Text = selectedAnimal.Name;
                SelectedAnimalEnclosure.Text = $"ID: {selectedAnimal.EnclosureId}";
                
                EventsList.ItemsSource = selectedAnimal.Events;
                
                DynamicAttributesList.ItemsSource = selectedAnimal.Attributes.ToList();
                
                DetailsArea.Visibility = Visibility.Visible;
                EditorArea.Visibility = Visibility.Collapsed;
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

            var db = new MySqlPersistenceService(DatabaseConfig.GetConnectionString());
            var newAnimal = new Core.Models.Animal
            {
                Name = NewAnimalName.Text,
                SpeciesId = ((Core.Models.Species)SpeciesSelector.SelectedItem).Id,
                EnclosureId = EnclosureSelector.SelectedItem != null ? ((Core.Models.Enclosure)EnclosureSelector.SelectedItem).Id : 0
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
                    Date = NewEventDate.SelectedDate.Value, // Datum vom Kalender nehmen
                    Type = NewEventType.Text,
                    Description = NewEventDesc.Text
                };

                var db = new MySqlPersistenceService(DatabaseConfig.GetConnectionString());
                db.AddAnimalEvent(selectedAnimal.Id, newEvent);

                // UI aktualisieren (Nach Datum sortiert einfügen)
                selectedAnimal.Events.Add(newEvent);
                var sortedEvents = selectedAnimal.Events.OrderByDescending(x => x.Date).ToList();
                selectedAnimal.Events = sortedEvents;
                EventsList.ItemsSource = sortedEvents;

                // Felder zurücksetzen
                NewEventType.Text = "";
                NewEventDesc.Text = "";
                NewEventDate.SelectedDate = System.DateTime.Now;
                
                ZooMessageBox.Show("Ereignis wurde in der digitalen Akte gespeichert.", "Erfolg");
            }
        }
    }
}