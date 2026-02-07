using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Models;
using ZooManager.UI.Views;

namespace ZooManager.UI.ViewModels
{
    public class AnimalsViewModel : ViewModelBase
    {
        private readonly IPersistenceService _persistenceService;
        private readonly IAuthenticationService? _authService;

        private Animal? _selectedAnimal;
        private bool _isEditorOpen;

        private string? _newAnimalName;
        private Species? _selectedSpecies;
        private Enclosure? _selectedEnclosure;
        private DateTime? _newAnimalFeedingDate;
        private string? _selectedFeedingHour;
        private string? _selectedFeedingMinute;

        private DateTime? _newEventDate;
        private string? _newEventType;
        private string? _newEventDesc;

        private AnimalEvent? _editingEvent;

        public ObservableCollection<Animal> Animals { get; } = new();
        public ObservableCollection<Species> SpeciesOptions { get; } = new();
        public ObservableCollection<Enclosure> EnclosureOptions { get; } = new();
        public ObservableCollection<AnimalEvent> Events { get; } = new();

        public ObservableCollection<string> FeedingHours { get; } = new();
        public ObservableCollection<string> FeedingMinutes { get; } = new();

        public bool IsEmployee => _authService?.GetCurrentUser()?.Role == UserRole.Employee;

        public bool CanManageAnimals => !IsEmployee;
        public bool CanManageEvents => _authService?.HasPermission("AddAnimalEvent") ?? false;

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

        public Animal? SelectedAnimal
        {
            get => _selectedAnimal;
            set
            {
                _selectedAnimal = value;
                OnPropertyChanged();

                OnPropertyChanged(nameof(SelectedAnimalName));
                OnPropertyChanged(nameof(SelectedAnimalSpecies));
                OnPropertyChanged(nameof(SelectedAnimalEnclosure));
                OnPropertyChanged(nameof(SelectedAnimalFeeding));
                OnPropertyChanged(nameof(DynamicAttributes));

                RefreshEventsFromSelected();
            }
        }

        public string SelectedAnimalName => SelectedAnimal?.Name ?? "-";
        public string SelectedAnimalSpecies => SelectedAnimal?.SpeciesName ?? "-";
        public string SelectedAnimalEnclosure => SelectedAnimal?.EnclosureName ?? "-";
        public string SelectedAnimalFeeding => SelectedAnimal?.NextFeedingTime.ToString("g") ?? "-";

        public IEnumerable<KeyValuePair<string, object>> DynamicAttributes =>
            SelectedAnimal?.Attributes?.ToList() ?? Enumerable.Empty<KeyValuePair<string, object>>();

        public string? NewAnimalName
        {
            get => _newAnimalName;
            set { _newAnimalName = value; OnPropertyChanged(); }
        }

        public Species? SelectedSpecies
        {
            get => _selectedSpecies;
            set { _selectedSpecies = value; OnPropertyChanged(); }
        }

        public Enclosure? SelectedEnclosure
        {
            get => _selectedEnclosure;
            set { _selectedEnclosure = value; OnPropertyChanged(); }
        }

        public DateTime? NewAnimalFeedingDate
        {
            get => _newAnimalFeedingDate;
            set { _newAnimalFeedingDate = value; OnPropertyChanged(); }
        }

        public string? SelectedFeedingHour
        {
            get => _selectedFeedingHour;
            set { _selectedFeedingHour = value; OnPropertyChanged(); }
        }

        public string? SelectedFeedingMinute
        {
            get => _selectedFeedingMinute;
            set { _selectedFeedingMinute = value; OnPropertyChanged(); }
        }

        public DateTime? NewEventDate
        {
            get => _newEventDate;
            set { _newEventDate = value; OnPropertyChanged(); }
        }

        public string? NewEventType
        {
            get => _newEventType;
            set { _newEventType = value; OnPropertyChanged(); }
        }

        public string? NewEventDesc
        {
            get => _newEventDesc;
            set { _newEventDesc = value; OnPropertyChanged(); }
        }

        public string EventEditorTitle => _editingEvent == null ? "Neues Ereignis erfassen" : "Ereignis bearbeiten";
        public string SaveEventButtonText => _editingEvent == null ? "+ In Akte speichern" : "Änderungen speichern";
        public bool IsCancelEditEventVisible => _editingEvent != null;

        public RelayCommand AddAnimalCommand { get; }
        public RelayCommand CancelEditorCommand { get; }
        public RelayCommand SaveNewAnimalCommand { get; }
        public RelayCommand DeleteAnimalCommand { get; }

        public RelayCommand AddOrUpdateEventCommand { get; }
        public RelayCommand EditEventCommand { get; }
        public RelayCommand CancelEditEventCommand { get; }
        public RelayCommand DeleteEventCommand { get; }

        public AnimalsViewModel(IPersistenceService persistenceService, IAuthenticationService? authService = null)
        {
            _persistenceService = persistenceService;
            _authService = authService;

            SeedTimePickers();

            AddAnimalCommand = new RelayCommand(_ => OpenEditor(), _ => CanManageAnimals);
            CancelEditorCommand = new RelayCommand(_ => CloseEditor());
            SaveNewAnimalCommand = new RelayCommand(_ => SaveNewAnimal(), _ => CanManageAnimals);
            DeleteAnimalCommand = new RelayCommand(_ => DeleteSelectedAnimal(), _ => CanManageAnimals);

            AddOrUpdateEventCommand = new RelayCommand(_ => AddOrUpdateEvent(), _ => CanManageEvents);
            EditEventCommand = new RelayCommand(p => EnterEditMode(p as AnimalEvent), _ => CanManageEvents);
            CancelEditEventCommand = new RelayCommand(_ => ExitEditMode());
            DeleteEventCommand = new RelayCommand(p => DeleteEvent(p as AnimalEvent), _ => CanManageEvents);

            LoadData();
            CloseEditor();
        }

        private void SeedTimePickers()
        {
            FeedingHours.Clear();
            FeedingMinutes.Clear();

            for (int i = 0; i < 24; i++)
                FeedingHours.Add(i.ToString("D2"));

            for (int i = 0; i < 60; i += 5)
                FeedingMinutes.Add(i.ToString("D2"));

            NewAnimalFeedingDate = DateTime.Today;
            SelectedFeedingHour = DateTime.Now.Hour.ToString("D2");
            SelectedFeedingMinute = ((DateTime.Now.Minute / 5) * 5).ToString("D2");

            NewEventDate = DateTime.Now;
        }

        private void LoadData()
        {
            Animals.Clear();
            foreach (var a in _persistenceService.LoadAnimals().ToList())
                Animals.Add(a);

            if (CanManageAnimals)
            {
                SpeciesOptions.Clear();
                foreach (var s in _persistenceService.LoadSpecies().ToList())
                    SpeciesOptions.Add(s);

                EnclosureOptions.Clear();
                foreach (var e in _persistenceService.LoadEnclosures().ToList())
                    EnclosureOptions.Add(e);
            }

            SelectedAnimal ??= Animals.FirstOrDefault();
        }

        private void RefreshEventsFromSelected()
        {
            Events.Clear();

            if (SelectedAnimal?.Events == null)
                return;

            foreach (var ev in SelectedAnimal.Events.OrderByDescending(x => x.Date))
                Events.Add(ev);
        }

        private void OpenEditor()
        {
            IsEditorOpen = true;
            NewAnimalName = string.Empty;

            SelectedSpecies = SpeciesOptions.FirstOrDefault();
            SelectedEnclosure = null;

            NewAnimalFeedingDate = DateTime.Today;
            SelectedFeedingHour = DateTime.Now.Hour.ToString("D2");
            SelectedFeedingMinute = ((DateTime.Now.Minute / 5) * 5).ToString("D2");
        }

        private void CloseEditor()
        {
            if (IsEmployee)
                IsEditorOpen = false;
            else
                IsEditorOpen = false;
        }

        private void SaveNewAnimal()
        {
            if (string.IsNullOrWhiteSpace(NewAnimalName) || SelectedSpecies == null)
            {
                ZooMessageBox.Show("Bitte Name und Tierart angeben.", "Eingabefehler");
                return;
            }

            var date = NewAnimalFeedingDate ?? DateTime.Now;

            var hour = int.TryParse(SelectedFeedingHour, out var h) ? h : 0;
            var minute = int.TryParse(SelectedFeedingMinute, out var m) ? m : 0;

            var combinedFeedingTime = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);

            var existingAnimals = _persistenceService.LoadAnimals().ToList();
            var newId = existingAnimals.Any() ? existingAnimals.Max(a => a.Id) + 1 : 1;

            var newAnimal = new Animal
            {
                Id = newId,
                Name = NewAnimalName.Trim(),
                SpeciesId = SelectedSpecies.Id,
                EnclosureId = SelectedEnclosure?.Id,
                NextFeedingTime = combinedFeedingTime
            };

            _persistenceService.SaveAnimals(new List<Animal> { newAnimal });

            ZooMessageBox.Show($"{newAnimal.Name} wurde erfolgreich angelegt!", "Erfolg");
            IsEditorOpen = false;
            LoadData();
        }

        private void DeleteSelectedAnimal()
        {
            if (IsEmployee)
            {
                ZooMessageBox.Show("Sie haben keine Berechtigung, Tiere zu löschen.", "Zugriff verweigert");
                return;
            }

            if (SelectedAnimal == null)
                return;

            _persistenceService.DeleteAnimal(SelectedAnimal.Id);

            ZooMessageBox.Show($"{SelectedAnimal.Name} wurde aus dem Bestand entfernt.", "Gelöscht");
            LoadData();
        }

        private void AddOrUpdateEvent()
        {
            if (!CanManageEvents)
            {
                ZooMessageBox.Show("Sie haben keine Berechtigung, Ereignisse zu bearbeiten.", "Zugriff verweigert");
                return;
            }

            if (SelectedAnimal == null)
                return;

            if (string.IsNullOrWhiteSpace(NewEventType) ||
                string.IsNullOrWhiteSpace(NewEventDesc) ||
                NewEventDate == null)
            {
                ZooMessageBox.Show("Bitte Datum, Typ und Beschreibung vollständig ausfüllen.", "Eingabe unvollständig");
                return;
            }

            var newEv = new AnimalEvent
            {
                Date = NewEventDate.Value,
                Type = NewEventType,
                Description = NewEventDesc
            };

            if (_editingEvent != null)
            {
                _persistenceService.UpdateAnimalEvent(SelectedAnimal.Id, _editingEvent, newEv);

                SelectedAnimal.Events.RemoveAll(x => IsSameEvent(x, _editingEvent));
                SelectedAnimal.Events.Insert(0, newEv);

                RefreshEventsFromSelected();
                ExitEditMode();

                ZooMessageBox.Show("Ereignis wurde aktualisiert.", "Erfolg");
                return;
            }

            _persistenceService.AddAnimalEvent(SelectedAnimal.Id, newEv);

            SelectedAnimal.Events.Insert(0, newEv);
            RefreshEventsFromSelected();

            NewEventType = string.Empty;
            NewEventDesc = string.Empty;
            NewEventDate = DateTime.Now;

            ZooMessageBox.Show("Ereignis wurde in der digitalen Akte gespeichert.", "Erfolg");
        }

        private void EnterEditMode(AnimalEvent? ev)
        {
            if (ev == null)
                return;

            if (!CanManageEvents)
            {
                ZooMessageBox.Show("Sie haben keine Berechtigung, Ereignisse zu bearbeiten.", "Zugriff verweigert");
                return;
            }

            _editingEvent = ev;

            OnPropertyChanged(nameof(EventEditorTitle));
            OnPropertyChanged(nameof(SaveEventButtonText));
            OnPropertyChanged(nameof(IsCancelEditEventVisible));

            NewEventDate = ev.Date;
            NewEventType = ev.Type;
            NewEventDesc = ev.Description;
        }

        private void ExitEditMode()
        {
            _editingEvent = null;

            OnPropertyChanged(nameof(EventEditorTitle));
            OnPropertyChanged(nameof(SaveEventButtonText));
            OnPropertyChanged(nameof(IsCancelEditEventVisible));

            NewEventType = string.Empty;
            NewEventDesc = string.Empty;
            NewEventDate = DateTime.Now;
        }

        private void DeleteEvent(AnimalEvent? ev)
        {
            if (!CanManageEvents)
            {
                ZooMessageBox.Show("Sie haben keine Berechtigung, Ereignisse zu löschen.", "Zugriff verweigert");
                return;
            }

            if (SelectedAnimal == null || ev == null)
                return;

            _persistenceService.DeleteAnimalEvent(SelectedAnimal.Id, ev);

            if (_editingEvent == ev)
                ExitEditMode();

            SelectedAnimal.Events.Remove(ev);
            RefreshEventsFromSelected();

            ZooMessageBox.Show("Ereignis wurde gelöscht.", "Gelöscht");
        }

        private static bool IsSameEvent(AnimalEvent a, AnimalEvent b)
        {
            return a.Date == b.Date
                   && (a.Type ?? string.Empty) == (b.Type ?? string.Empty)
                   && (a.Description ?? string.Empty) == (b.Description ?? string.Empty);
        }
    }
}