using System.Collections.ObjectModel;
using System.Windows.Input;
using ZooManager.Core.Models;
using ZooManager.Core.Interfaces;

namespace ZooManager.UI.ViewModels
{
    public class SpeciesViewModel : ViewModelBase
    {
        private readonly IZooRepository _repository;
        private Species _selectedSpecies;

        public ObservableCollection<Species> SpeciesList { get; } = new();

        public Species SelectedSpecies
        {
            get => _selectedSpecies;
            set { _selectedSpecies = value; OnPropertyChanged(); }
        }

        public ICommand DeleteCommand { get; }

        public SpeciesViewModel(IZooRepository repository)
        {
            _repository = repository;
            DeleteCommand = new RelayCommand(DeleteSpecies, CanDeleteSpecies);
            LoadData();
        }

        private void LoadData()
        {
            SpeciesList.Clear();
            foreach (var s in _repository.GetAllSpecies()) SpeciesList.Add(s);
        }

        private void DeleteSpecies(object obj)
        {
            if (_repository.HasAnimalsForSpecies(SelectedSpecies.Id))
            {
                return;
            }

            _repository.DeleteSpecies(SelectedSpecies.Id);
            LoadData();
        }

        private bool CanDeleteSpecies(object obj) => true;
    }
}