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
    public partial class SpeciesView : UserControl
    {
        private SqlitePersistenceService _db;

        public SpeciesView(IPersistenceService persistenceService = null)
        {
            InitializeComponent();
            _db = persistenceService as SqlitePersistenceService ?? 
                  new SqlitePersistenceService(DatabaseConfig.GetConnectionString());
            LoadData();
        }

        private void LoadData()
        {
            SpeciesList.ItemsSource = _db.LoadSpecies().ToList();
        }

        private void SpeciesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SpeciesList.SelectedItem is Species selected)
            {
                SelectedSpeciesName.Text = selected.Name;
                SelectedSpeciesClimate.Text = $"Klima: {selected.RequiredClimate} | Min. Platz: {selected.MinSpacePerAnimal}qm";
                
                ConfiguredFieldsList.ItemsSource = selected.CustomFields;
                
                SpeciesDetailsArea.Visibility = Visibility.Visible;
                SpeciesEditorArea.Visibility = Visibility.Collapsed;
            }
        }

        private void AddSpecies_Click(object sender, RoutedEventArgs e)
        {
            SpeciesDetailsArea.Visibility = Visibility.Collapsed;
            SpeciesEditorArea.Visibility = Visibility.Visible;
            NewSpeciesName.Text = "";
            NewSpeciesSpace.Text = "";
            NewSpeciesClimate.SelectedIndex = -1;
        }

        private void CancelSpeciesEditor_Click(object sender, RoutedEventArgs e)
        {
            SpeciesEditorArea.Visibility = Visibility.Collapsed;
            SpeciesDetailsArea.Visibility = Visibility.Visible;
        }

        private void SaveNewSpecies_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewSpeciesName.Text)) return;

            var existingSpecies = _db.LoadSpecies().ToList();
            int newId = existingSpecies.Any() ? existingSpecies.Max(s => s.Id) + 1 : 1;

            var newSpecies = new Species
            {
                Id = newId,
                Name = NewSpeciesName.Text,
                RequiredClimate = (NewSpeciesClimate.SelectedItem as ComboBoxItem)?.Content.ToString(),
                NeedsWater = NewSpeciesNeedsWater.IsChecked == true,
                MinSpacePerAnimal = double.TryParse(NewSpeciesSpace.Text, out double s) ? s : 0
            };

            _db.SaveSpecies(new List<Species> { newSpecies });
            ZooMessageBox.Show($"Tierart '{newSpecies.Name}' wurde gespeichert.", "Erfolg");
            CancelSpeciesEditor_Click(null, null);
            LoadData();
        }

        private void DeleteSpecies_Click(object sender, RoutedEventArgs e)
        {
            if (SpeciesList.SelectedItem is Species selected)
            {
                // Prüfen, ob noch Tiere dieser Art existieren
                if (_db.LoadAnimals().Any(a => a.SpeciesId == selected.Id))
                {
                    ZooMessageBox.Show("Löschen nicht möglich: Es existieren noch Tiere dieser Art.", "Fehler");
                    return;
                }

                try
                {
                    _db.DeleteSpecies(selected.Id);
                    ZooMessageBox.Show("Tierart gelöscht.", "Info");
                    LoadData();
                    
                    // UI zurücksetzen
                    SpeciesDetailsArea.Visibility = Visibility.Collapsed;
                }
                catch (System.Exception ex)
                {
                    ZooMessageBox.Show($"Fehler beim Löschen: {ex.Message}", "Datenbankfehler");
                }
            }
            else
            {
                ZooMessageBox.Show("Bitte wählen Sie zuerst eine Tierart aus der Liste aus.", "Hinweis");
            }
        }
    }
}