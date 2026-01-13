namespace ZooManager.UI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ViewModelBase? _currentView;

        public ViewModelBase? CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand ShowSpeciesEditorCommand { get; }
        public RelayCommand ShowAnimalRecordCommand { get; }

        public MainWindowViewModel()
        {
            ShowSpeciesEditorCommand = new RelayCommand(_ =>
                CurrentView = new SpeciesEditorViewModel());

            ShowAnimalRecordCommand = new RelayCommand(_ =>
                CurrentView = new AnimalRecordViewModel());
        }
    }
}