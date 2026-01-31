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
        private IPersistenceService _persistenceService;
        private IAuthenticationService _authService;

        public EmployeesView(IPersistenceService persistenceService, IAuthenticationService authService)
        {
            InitializeComponent();

            _persistenceService = persistenceService;
            _authService = authService;

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
            var role = _authService.GetCurrentUser()?.Role;
            if (role == UserRole.Employee)
            {
                ZooMessageBox.Show("Sie haben keine Berechtigung, Mitarbeiter anzulegen.", "Zugriff verweigert");
                return;
            }

            if (NewEmpUsername == null || NewEmpPassword == null)
            {
                ZooMessageBox.Show("Login-Felder fehlen im XAML (NewEmpUsername/NewEmpPassword). Bitte XAML prüfen.", "Konfigurationsfehler");
                return;
            }

            if (string.IsNullOrWhiteSpace(NewEmpLastName?.Text))
                return;

            if (string.IsNullOrWhiteSpace(NewEmpUsername.Text) || string.IsNullOrWhiteSpace(NewEmpPassword.Password))
            {
                ZooMessageBox.Show("Bitte Benutzername und Passwort für den Login angeben.", "Eingabefehler");
                return;
            }

            var newEmp = new Employee
            {
                FirstName = NewEmpFirstName?.Text ?? "",
                LastName = NewEmpLastName.Text
            };

            _persistenceService.SaveEmployees(new List<Employee> { newEmp });

            if (newEmp.Id <= 0)
            {
                ZooMessageBox.Show("Mitarbeiter konnte nicht gespeichert werden (keine ID erhalten).", "Fehler");
                return;
            }

            var created = _authService.CreateUser(
                NewEmpUsername.Text.Trim(),
                NewEmpPassword.Password,
                UserRole.Employee,
                newEmp.Id);

            if (!created)
            {
                ZooMessageBox.Show("Benutzername existiert bereits oder Benutzer konnte nicht erstellt werden.", "Login-Fehler");
                return;
            }

            ZooMessageBox.Show("Mitarbeiter + Login wurde erfolgreich angelegt.", "Erfolg");

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