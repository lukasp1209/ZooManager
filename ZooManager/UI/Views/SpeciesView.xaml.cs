using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ZooManager.Core.Models;
using ZooManager.Infrastructure.Persistence;
using ZooManager.Infrastructure.Configuration;

namespace ZooManager.UI.Views
{
    public partial class SpeciesView : UserControl
    {
        public SpeciesView()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            var db = new MySqlPersistenceService(DatabaseConfig.GetConnectionString());
            SpeciesList.ItemsSource = db.LoadSpecies().ToList();
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

            var db = new MySqlPersistenceService(DatabaseConfig.GetConnectionString());
            var newSpecies = new Species
            {
                Name = NewSpeciesName.Text,
                RequiredClimate = (NewSpeciesClimate.SelectedItem as ComboBoxItem)?.Content.ToString(),
                MinSpacePerAnimal = double.TryParse(NewSpeciesSpace.Text, out double s) ? s : 0
            };

            db.SaveSpecies(new List<Species> { newSpecies });
            ZooMessageBox.Show($"Tierart '{newSpecies.Name}' wurde gespeichert.", "Erfolg");
            CancelSpeciesEditor_Click(null, null);
            LoadData();
        }

        private void DeleteSpecies_Click(object sender, RoutedEventArgs e)
        {
            if (SpeciesList.SelectedItem is Species selected)
            {
                var db = new MySqlPersistenceService(DatabaseConfig.GetConnectionString());
                // Prüfen, ob noch Tiere dieser Art existieren
                if (db.LoadAnimals().Any(a => a.SpeciesId == selected.Id))
                {
                    ZooMessageBox.Show("Löschen nicht möglich: Es existieren noch Tiere dieser Art.", "Fehler");
                    return;
                }

                using (var conn = new MySql.Data.MySqlClient.MySqlConnection(DatabaseConfig.GetConnectionString()))
                {
                    conn.Open();
                    var cmd = new MySql.Data.MySqlClient.MySqlCommand("DELETE FROM Species WHERE Id = @id", conn);
                    cmd.Parameters.AddWithValue("@id", selected.Id);
                    cmd.ExecuteNonQuery();
                }

                ZooMessageBox.Show("Tierart gelöscht.", "Info");
                LoadData();
            }
        }
    }
}