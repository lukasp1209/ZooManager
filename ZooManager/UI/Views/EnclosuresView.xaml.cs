using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ZooManager.Core.Models;
using ZooManager.Core.Services;
using ZooManager.Infrastructure.Persistence;
using ZooManager.Infrastructure.Configuration;

namespace ZooManager.UI.Views
{
    public partial class EnclosuresView : UserControl
    {
        private List<Enclosure> _enclosures;
        private List<Animal> _allAnimals;
        private List<Species> _species;
        private EnclosureValidationService _validator = new EnclosureValidationService();

        public EnclosuresView()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            var db = new SqlitePersistenceService(DatabaseConfig.GetConnectionString());
            _enclosures = db.LoadEnclosures().ToList();
            _allAnimals = db.LoadAnimals().ToList();
            _species = db.LoadSpecies().ToList();

            EnclosureList.ItemsSource = _enclosures;
            AllAnimalsSelector.ItemsSource = _allAnimals;
        }

        private void EnclosureList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EnclosureList.SelectedItem is Enclosure selected)
            {
                SelectedEnclosureTitle.Text = selected.Name;
                DetailClimate.Text = selected.ClimateType;
                DetailArea.Text = $"{selected.TotalArea} qm";
                DetailCapacity.Text = $"{selected.MaxCapacity} Tiere";

                EnclosureDetailsArea.Visibility = Visibility.Visible;
                EnclosureEditorArea.Visibility = Visibility.Collapsed;
            }
        }

        private void AssignAnimal_Click(object sender, RoutedEventArgs e)
        {
            if (EnclosureList.SelectedItem is Enclosure selectedEnclosure && 
                AllAnimalsSelector.SelectedItem is Animal selectedAnimal)
            {
                var species = _species.FirstOrDefault(s => s.Id == selectedAnimal.SpeciesId);
                int currentCount = _allAnimals.Count(a => a.EnclosureId == selectedEnclosure.Id);

                // ANF2: Validierung
                if (_validator.IsSuitable(selectedAnimal, species, selectedEnclosure, currentCount))
                {
                    selectedAnimal.EnclosureId = selectedEnclosure.Id;
                    ZooMessageBox.Show($"{selectedAnimal.Name} wurde erfolgreich zugeordnet.", "Erfolg");
                }
                else
                {
                    string reason = _validator.GetIncompatibilityReason(selectedAnimal, species, selectedEnclosure, currentCount);
                    ZooMessageBox.Show(reason, "Validierungsfehler");
                }
            }
        }

        private void AddEnclosure_Click(object sender, RoutedEventArgs e)
        {
            EnclosureDetailsArea.Visibility = Visibility.Collapsed;
            EnclosureEditorArea.Visibility = Visibility.Visible;
            NewEnclosureName.Text = "";
            NewEnclosureArea.Text = "";
            NewEnclosureCapacity.Text = "";
        }

        private void CancelEnclosureEditor_Click(object sender, RoutedEventArgs e)
        {
            EnclosureEditorArea.Visibility = Visibility.Collapsed;
            EnclosureDetailsArea.Visibility = Visibility.Visible;
        }

        private void SaveNewEnclosure_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewEnclosureName.Text) || NewEnclosureClimate.SelectedItem == null)
            {
                ZooMessageBox.Show("Bitte Name und Klima angeben.", "Eingabefehler");
                return;
            }

            var db = new SqlitePersistenceService(DatabaseConfig.GetConnectionString());
            var newEnc = new Enclosure
            {
                Name = NewEnclosureName.Text,
                ClimateType = (NewEnclosureClimate.SelectedItem as ComboBoxItem).Content.ToString(),
                TotalArea = double.TryParse(NewEnclosureArea.Text, out double a) ? a : 0,
                MaxCapacity = int.TryParse(NewEnclosureCapacity.Text, out int c) ? c : 1
            };

            db.SaveEnclosures(new List<Enclosure> { newEnc });
            
            ZooMessageBox.Show($"Anlage '{newEnc.Name}' wurde erstellt.", "Erfolg");
            CancelEnclosureEditor_Click(null, null);
            LoadData();
        }

        private void DeleteEnclosure_Click(object sender, RoutedEventArgs e)
        {
            if (EnclosureList.SelectedItem is Enclosure selected)
            {
                // Prüfen, ob noch Tiere im Gehege sind (Wichtig für ANF2)
                if (_allAnimals.Any(a => a.EnclosureId == selected.Id))
                {
                    ZooMessageBox.Show("Anlage kann nicht gelöscht werden, da sie noch bewohnt ist!", "Sperre");
                    return;
                }

                using (var conn = new MySql.Data.MySqlClient.MySqlConnection(DatabaseConfig.GetConnectionString()))
                {
                    conn.Open();
                    var cmd = new MySql.Data.MySqlClient.MySqlCommand("DELETE FROM Enclosures WHERE Id = @id", conn);
                    cmd.Parameters.AddWithValue("@id", selected.Id);
                    cmd.ExecuteNonQuery();
                }

                ZooMessageBox.Show("Anlage wurde gelöscht.", "Info");
                LoadData();
            }
        }
    }
}