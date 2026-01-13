namespace ZooManager.UI.ViewModels
{
    public class SpeciesEditorViewModel : ViewModelBase
    {
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

        public SpeciesEditorViewModel()
        {
            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        private void Save()
        {
            // TODO: Persist species (DB / API)
        }

        private void Cancel()
        {
            // TODO: Close editor / navigate back
        }
    }
}