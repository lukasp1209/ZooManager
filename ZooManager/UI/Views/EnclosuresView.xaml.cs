using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Models;
using ZooManager.Core.Services;
using ZooManager.Infrastructure.Persistence;
using ZooManager.Infrastructure.Configuration;
using ZooManager.Infrastructure.Persistence.Connection;

namespace ZooManager.UI.Views
{
    public partial class EnclosuresView : UserControl
    {
        private List<Enclosure> _enclosures;
        private List<Animal> _allAnimals;
        private List<Species> _species;
        private EnclosureValidationService _validator = new EnclosureValidationService();
        private SqlitePersistenceService _db;

        public EnclosuresView(IPersistenceService persistenceService = null)
        {
            InitializeComponent();
            _db = persistenceService as SqlitePersistenceService ?? 
                  new SqlitePersistenceService(DatabaseConfig.GetConnectionString());
            LoadData();
        }

        private void LoadData()
        {
            _enclosures = _db.LoadEnclosures().ToList();
            _allAnimals = _db.LoadAnimals().ToList();
            _species = _db.LoadSpecies().ToList();

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
                    _db.SaveAnimals(new List<Animal> { selectedAnimal });
                    LoadData(); // Daten neu laden
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

            var existingEnclosures = _db.LoadEnclosures().ToList();
            int newId = existingEnclosures.Any() ? existingEnclosures.Max(enc => enc.Id) + 1 : 1;

            var newEnc = new Enclosure
            {
                Id = newId,
                Name = NewEnclosureName.Text,
                ClimateType = (NewEnclosureClimate.SelectedItem as ComboBoxItem).Content.ToString(),
                HasWaterAccess = NewEnclosureHasWater.IsChecked == true,
                TotalArea = double.TryParse(NewEnclosureArea.Text, out double a) ? a : 0,
                MaxCapacity = int.TryParse(NewEnclosureCapacity.Text, out int c) ? c : 1
            };

            _db.SaveEnclosures(new List<Enclosure> { newEnc });
            
            ZooMessageBox.Show($"Anlage '{newEnc.Name}' wurde erstellt.", "Erfolg");
            CancelEnclosureEditor_Click(null, null);
            LoadData();
        }

        private void DeleteEnclosure_Click(object sender, RoutedEventArgs e)
        {
            if (EnclosureList.SelectedItem is Enclosure selected)
            {
                if (_allAnimals.Any(a => a.EnclosureId == selected.Id))
                {
                    ZooMessageBox.Show("Anlage kann nicht gelöscht werden, da sie noch bewohnt ist!", "Sperre");
                    return;
                }

                try
                {
                    _db.DeleteEnclosure(selected.Id);
                    ZooMessageBox.Show("Anlage wurde gelöscht.", "Info");
                    LoadData();
                    
                    // UI zurücksetzen
                    EnclosureDetailsArea.Visibility = Visibility.Collapsed;
                }
                catch (System.Exception ex)
                {
                    ZooMessageBox.Show($"Fehler beim Löschen: {ex.Message}", "Datenbankfehler");
                }
            }
            else
            {
                ZooMessageBox.Show("Bitte wählen Sie zuerst eine Anlage aus der Liste aus.", "Hinweis");
            }
        }
    }
}