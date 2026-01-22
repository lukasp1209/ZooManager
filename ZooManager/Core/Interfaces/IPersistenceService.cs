using System.Collections.Generic;
using ZooManager.Core.Models;

namespace ZooManager.Core.Interfaces
{
    public interface IPersistenceService
    {
        IEnumerable<Animal> LoadAnimals();
        void SaveAnimals(IEnumerable<Animal> animals);

        IEnumerable<Species> LoadSpecies();
        void SaveSpecies(IEnumerable<Species> species);

        IEnumerable<Enclosure> LoadEnclosures();
        void SaveEnclosures(IEnumerable<Enclosure> enclosures);

        IEnumerable<Employee> LoadEmployees();
        void SaveEmployees(IEnumerable<Employee> employees);

        IEnumerable<ZooEvent> LoadEvents();
        void SaveEvents(IEnumerable<ZooEvent> events);
        
        User? GetUserByUsername(string username);
        User? GetUserById(int id);
        bool SaveUser(User user);
    }

    public interface IValidationService
    {
        bool ValidateAnimal(Animal animal, out IEnumerable<string> errors);
        bool ValidateEnclosure(Enclosure enclosure, out IEnumerable<string> errors);
        bool ValidateEvent(ZooEvent zooEvent, out IEnumerable<string> errors);
    }
}