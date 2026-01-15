using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ZooManager.Core.Models;
using ZooManager.Infrastructure.Persistence;
using ZooManager.Infrastructure.Configuration;

namespace ZooManager.UI.Views
{
    public partial class EmployeesView : UserControl
    {
        private List<Employee> _employees;
        private List<Species> _allSpecies;
        private MySqlPersistenceService _db;

        public EmployeesView()
        {
            InitializeComponent();
            _db = new MySqlPersistenceService(DatabaseConfig.GetConnectionString());
            LoadData();
        }

        private void LoadData()
        {
            _employees = _db.LoadEmployees().ToList();
            _allSpecies = _db.LoadSpecies().ToList();
            EmployeesList.ItemsSource = _employees;
        }

        private void EmployeesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EmployeesList.SelectedItem is Employee selectedEmployee)
            {
                SelectedEmployeeTitle.Text = $"{selectedEmployee.FirstName} {selectedEmployee.LastName}";
                
                var qualifications = _allSpecies.Select(s => new SpeciesQualification
                {
                    SpeciesId = s.Id,
                    SpeciesName = s.Name,
                    IsQualified = selectedEmployee.QualifiedSpeciesIds.Contains(s.Id)
                }).ToList();

                QualificationList.ItemsSource = qualifications;
                
                EmployeeDetailsArea.Visibility = Visibility.Visible;
                EmployeeEditorArea.Visibility = Visibility.Collapsed;
            }
        }

        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            EmployeeDetailsArea.Visibility = Visibility.Collapsed;
            EmployeeEditorArea.Visibility = Visibility.Visible;
            NewEmpFirstName.Text = "";
            NewEmpLastName.Text = "";
        }

        private void CancelEmployeeEditor_Click(object sender, RoutedEventArgs e)
        {
            EmployeeEditorArea.Visibility = Visibility.Collapsed;
            EmployeeDetailsArea.Visibility = Visibility.Visible;
        }

        private void SaveNewEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewEmpLastName.Text)) return;

            var newEmp = new Employee 
            { 
                FirstName = NewEmpFirstName.Text, 
                LastName = NewEmpLastName.Text 
            };

            _db.SaveEmployees(new List<Employee> { newEmp });
            ZooMessageBox.Show("Mitarbeiter erfolgreich angelegt.", "Personalwesen");
            CancelEmployeeEditor_Click(null, null);
            LoadData();
        }

        private void DeleteEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeesList.SelectedItem is Employee selected)
            {
                using (var conn = new MySql.Data.MySqlClient.MySqlConnection(DatabaseConfig.GetConnectionString()))
                {
                    conn.Open();
                    var cmd = new MySql.Data.MySqlClient.MySqlCommand("DELETE FROM Employees WHERE Id = @id", conn);
                    cmd.Parameters.AddWithValue("@id", selected.Id);
                    cmd.ExecuteNonQuery();
                }
                ZooMessageBox.Show("Mitarbeiter gelöscht.", "Info");
                LoadData();
            }
        }

        private void QualificationCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeesList.SelectedItem is Employee selectedEmployee && 
                sender is CheckBox cb && cb.DataContext is SpeciesQualification qual)
            {
                // Update im lokalen Objekt
                if (cb.IsChecked == true)
                    selectedEmployee.QualifiedSpeciesIds.Add(qual.SpeciesId);
                else
                    selectedEmployee.QualifiedSpeciesIds.Remove(qual.SpeciesId);

                // ANF3: Sofort in DB persistieren
                _db.SaveEmployeeQualifications(selectedEmployee.Id, selectedEmployee.QualifiedSpeciesIds);
                
                // Professionelle Rückmeldung
                // ZooMessageBox.Show($"Qualifikation für {qual.SpeciesName} aktualisiert.", "Personalverwaltung");
            }
        }
    }
}
