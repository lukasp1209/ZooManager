using System;
using System.Linq;
using System.Windows;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Models;
using ZooManager.Infrastructure.Configuration;
using ZooManager.Infrastructure.Persistence.Connection;

namespace ZooManager.UI.ViewModels
{
    public class SpeciesEditorViewModel : ViewModelBase
    {
        private readonly IPersistenceService _persistenceService;
        private readonly Action _navigateBack;

        private string? _name;
        private string? _scientificName;
        private string? _category;
        private string? _description;

        public string? Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string? ScientificName
        {
            get => _scientificName;
            set { _scientificName = value; OnPropertyChanged(); }
        }

        public string? Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(); }
        }

        public string? Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        public SpeciesEditorViewModel(
            IPersistenceService? persistenceService = null,
            Action? navigateBack = null)
        {
            _persistenceService = persistenceService
                                 ?? new SqlitePersistenceService(DatabaseConfig.GetConnectionString());

            _navigateBack = navigateBack ?? (() => { });

            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                MessageBox.Show("Bitte gib einen Namen für die Tierart ein.", "Validierung");
                return;
            }
            
            var existing = _persistenceService.LoadSpecies().ToList();
            var newId = existing.Any() ? existing.Max(s => s.Id) + 1 : 1;

            var entity = new Species
            {
                Id = newId,
                Name = Name.Trim(),
                RequiredClimate = string.IsNullOrWhiteSpace(Category) ? null : Category.Trim(),
                NeedsWater = false,
                MinSpacePerAnimal = 0
            };

            _persistenceService.SaveSpecies(new[] { entity });

            MessageBox.Show($"Tierart '{entity.Name}' wurde gespeichert.", "Erfolg");
            _navigateBack();
        }

        private void Cancel()
        {
            _navigateBack();
        }
    }
}