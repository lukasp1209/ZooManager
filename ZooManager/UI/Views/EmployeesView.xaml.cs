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
    public partial class EmployeesView : UserControl
    {
        private List<Employee> _employees;
        private List<Species> _allSpecies;
        private SqlitePersistenceService _db;

        public EmployeesView(IPersistenceService persistenceService = null)
        {
            InitializeComponent();
            _db = persistenceService as SqlitePersistenceService ?? 
                  new SqlitePersistenceService(DatabaseConfig.GetConnectionString());
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

            var existingEmployees = _db.LoadEmployees().ToList();
            int newId = existingEmployees.Any() ? existingEmployees.Max(emp => emp.Id) + 1 : 1;

            var newEmp = new Employee 
            { 
                Id = newId,
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
                try
                {
                    _db.DeleteEmployee(selected.Id);
                    ZooMessageBox.Show("Mitarbeiter und zugehörige Qualifikationen wurden gelöscht.", "Personalverwaltung");
                    LoadData();
                    
                    // UI zurücksetzen
                    EmployeeDetailsArea.Visibility = Visibility.Collapsed;
                }
                catch (System.Exception ex)
                {
                    ZooMessageBox.Show($"Fehler beim Löschen: {ex.Message}", "Datenbankfehler");
                }
            }
            else
            {
                ZooMessageBox.Show("Bitte wählen Sie zuerst einen Mitarbeiter aus der Liste aus.", "Hinweis");
            }
        }

        private void QualificationCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeesList.SelectedItem is Employee selectedEmployee && 
                sender is CheckBox cb && cb.DataContext is SpeciesQualification qual)
            {
                if (cb.IsChecked == true)
                    selectedEmployee.QualifiedSpeciesIds.Add(qual.SpeciesId);
                else
                    selectedEmployee.QualifiedSpeciesIds.Remove(qual.SpeciesId);

                _db.SaveEmployeeQualifications(selectedEmployee.Id, selectedEmployee.QualifiedSpeciesIds);
            }
        }
    }
}
