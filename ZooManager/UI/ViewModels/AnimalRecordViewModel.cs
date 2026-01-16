namespace ZooManager.UI.ViewModels
{
    public class AnimalRecordViewModel : ViewModelBase
    {
        private string? _animalName;
        private string? _species;
        private string? _gender;
        private DateTime? _dateOfBirth;
        private string? _enclosure;
        private string? _status;

        public string? AnimalName
        {
            get => _animalName;
            set { _animalName = value; OnPropertyChanged(); }
        }

        public string? Species
        {
            get => _species;
            set { _species = value; OnPropertyChanged(); }
        }

        public string? Gender
        {
            get => _gender;
            set { _gender = value; OnPropertyChanged(); }
        }

        public DateTime? DateOfBirth
        {
            get => _dateOfBirth;
            set { _dateOfBirth = value; OnPropertyChanged(); }
        }

        public string? Enclosure
        {
            get => _enclosure;
            set { _enclosure = value; OnPropertyChanged(); }
        }

        public string? Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }
    }
}