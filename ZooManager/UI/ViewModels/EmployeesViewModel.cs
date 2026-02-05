using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Models;
using ZooManager.Infrastructure.Persistence.Connection;
using ZooManager.UI.Views;

namespace ZooManager.UI.ViewModels

{
    public class EmployeesViewModel : ViewModelBase
    {
        private readonly IPersistenceService _persistenceService;
        private readonly IAuthenticationService _authService;
        private readonly SqlitePersistenceService? _sqliteDb;

        private Employee? _selectedEmployee;
        private bool _isEditorOpen;

        private string? _newEmpFirstName;
        private string? _newEmpLastName;
        private string? _newEmpUsername;
        private string? _newEmpPassword;

        private List<Species> _allSpecies = new();

        public ObservableCollection<Employee> Employees { get; } = new();
        public ObservableCollection<SpeciesQualificationItem> Qualifications { get; } = new();

        public Employee? SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                _selectedEmployee = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedEmployeeTitle));

                BuildQualifications();
                IsEditorOpen = false;
            }
        }

        public string SelectedEmployeeTitle =>
            SelectedEmployee == null
                ? "Wählen Sie einen Mitarbeiter"
                : $"{SelectedEmployee.FirstName} {SelectedEmployee.LastName}";

        public bool CanManageEmployees => _authService.GetCurrentUser()?.Role != UserRole.Employee;

        public bool IsEditorOpen
        {
            get => _isEditorOpen;
            set
            {
                _isEditorOpen = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDetailsOpen));
            }
        }

        public bool IsDetailsOpen => !IsEditorOpen;

        public string? NewEmpFirstName
        {
            get => _newEmpFirstName;
            set { _newEmpFirstName = value; OnPropertyChanged(); }
        }

        public string? NewEmpLastName
        {
            get => _newEmpLastName;
            set { _newEmpLastName = value; OnPropertyChanged(); }
        }

        public string? NewEmpUsername
        {
            get => _newEmpUsername;
            set { _newEmpUsername = value; OnPropertyChanged(); }
        }

        // wird über Code-Behind gesetzt (PasswordBox)
        public string? NewEmpPassword
        {
            get => _newEmpPassword;
            set { _newEmpPassword = value; OnPropertyChanged(); }
        }

        public RelayCommand AddEmployeeCommand { get; }
        public RelayCommand CancelEmployeeEditorCommand { get; }
        public RelayCommand SaveNewEmployeeCommand { get; }
        public RelayCommand DeleteEmployeeCommand { get; }
        public RelayCommand ToggleQualificationCommand { get; }

        public EmployeesViewModel(IPersistenceService persistenceService, IAuthenticationService authService)
        {
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            _sqliteDb = _persistenceService as SqlitePersistenceService;

            AddEmployeeCommand = new RelayCommand(_ => OpenEditor(), _ => CanManageEmployees);
            CancelEmployeeEditorCommand = new RelayCommand(_ => CloseEditor());
            SaveNewEmployeeCommand = new RelayCommand(_ => SaveNewEmployee(), _ => CanManageEmployees);
            DeleteEmployeeCommand = new RelayCommand(_ => DeleteSelectedEmployee(), _ => CanManageEmployees);

            ToggleQualificationCommand = new RelayCommand(
                p => ToggleQualification(p as SpeciesQualificationItem),
                _ => SelectedEmployee != null);

            LoadData();
        }

        private void LoadData()
        {
            Employees.Clear();
            foreach (var e in _persistenceService.LoadEmployees().ToList())
                Employees.Add(e);

            _allSpecies = _persistenceService.LoadSpecies().ToList();

            if (SelectedEmployee != null)
            {
                SelectedEmployee = Employees.FirstOrDefault(x => x.Id == SelectedEmployee.Id);
            }
            else
            {
                SelectedEmployee = Employees.FirstOrDefault();
            }
        }

        private void BuildQualifications()
        {
            Qualifications.Clear();

            if (SelectedEmployee == null)
                return;

            var qualified = SelectedEmployee.QualifiedSpeciesIds ?? new List<int>();

            foreach (var s in _allSpecies)
            {
                Qualifications.Add(new SpeciesQualificationItem
                {
                    SpeciesId = s.Id,
                    SpeciesName = s.Name,
                    IsQualified = qualified.Contains(s.Id)
                });
            }
        }

        private void OpenEditor()
        {
            IsEditorOpen = true;

            NewEmpFirstName = string.Empty;
            NewEmpLastName = string.Empty;
            NewEmpUsername = string.Empty;
            NewEmpPassword = string.Empty;
        }

        private void CloseEditor()
        {
            IsEditorOpen = false;
        }

        private void SaveNewEmployee()
        {
            if (!CanManageEmployees)
            {
                ZooMessageBox.Show("Sie haben keine Berechtigung, Mitarbeiter anzulegen.", "Zugriff verweigert");
                return;
            }

            if (string.IsNullOrWhiteSpace(NewEmpLastName))
                return;

            if (string.IsNullOrWhiteSpace(NewEmpUsername) || string.IsNullOrWhiteSpace(NewEmpPassword))
            {
                ZooMessageBox.Show("Bitte Benutzername und Passwort für den Login angeben.", "Eingabefehler");
                return;
            }

            var newEmp = new Employee
            {
                FirstName = NewEmpFirstName?.Trim() ?? string.Empty,
                LastName = NewEmpLastName.Trim()
            };

            _persistenceService.SaveEmployees(new List<Employee> { newEmp });

            if (newEmp.Id <= 0)
            {
                ZooMessageBox.Show("Mitarbeiter konnte nicht gespeichert werden (keine ID erhalten).", "Fehler");
                return;
            }

            var created = _authService.CreateUser(
                NewEmpUsername.Trim(),
                NewEmpPassword,
                UserRole.Employee,
                newEmp.Id);

            if (!created)
            {
                ZooMessageBox.Show("Benutzername existiert bereits oder Benutzer konnte nicht erstellt werden.", "Login-Fehler");
                return;
            }

            ZooMessageBox.Show("Mitarbeiter + Login wurde erfolgreich angelegt.", "Erfolg");

            CloseEditor();
            LoadData();
        }

        private void DeleteSelectedEmployee()
        {
            if (!CanManageEmployees)
            {
                ZooMessageBox.Show("Sie haben keine Berechtigung, Mitarbeiter zu löschen.", "Zugriff verweigert");
                return;
            }

            if (SelectedEmployee == null)
            {
                ZooMessageBox.Show("Bitte wählen Sie zuerst einen Mitarbeiter aus der Liste aus.", "Hinweis");
                return;
            }

            if (_sqliteDb == null)
            {
                ZooMessageBox.Show(
                    "Mitarbeiter können nicht gelöscht werden: PersistenceService ist kein SqlitePersistenceService.",
                    "Konfigurationsfehler");
                return;
            }

            try
            {
                _sqliteDb.DeleteEmployee(SelectedEmployee.Id);
                ZooMessageBox.Show("Mitarbeiter und zugehörige Qualifikationen wurden gelöscht.", "Personalverwaltung");

                SelectedEmployee = null;
                LoadData();
            }
            catch (Exception ex)
            {
                ZooMessageBox.Show($"Fehler beim Löschen: {ex.Message}", "Datenbankfehler");
            }
        }

        private void ToggleQualification(SpeciesQualificationItem? item)
        {
            if (SelectedEmployee == null || item == null)
                return;

            if (_sqliteDb == null)
            {
                ZooMessageBox.Show(
                    "Qualifikationen können nicht gespeichert werden: PersistenceService ist kein SqlitePersistenceService.",
                    "Konfigurationsfehler");
                return;
            }

            if (item.IsQualified)
            {
                if (!SelectedEmployee.QualifiedSpeciesIds.Contains(item.SpeciesId))
                    SelectedEmployee.QualifiedSpeciesIds.Add(item.SpeciesId);
            }
            else
            {
                SelectedEmployee.QualifiedSpeciesIds.Remove(item.SpeciesId);
            }

            _sqliteDb.SaveEmployeeQualifications(SelectedEmployee.Id, SelectedEmployee.QualifiedSpeciesIds);
        }
    }

    public class SpeciesQualificationItem : ViewModelBase
    {
        private bool _isQualified;

        public int SpeciesId { get; set; }
        public string SpeciesName { get; set; } = string.Empty;

        public bool IsQualified
        {
            get => _isQualified;
            set { _isQualified = value; OnPropertyChanged(); }
        }
    }
}