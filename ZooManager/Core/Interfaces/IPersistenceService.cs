using System.Collections.Generic;
using ZooManager.Core.Models;

namespace ZooManager.Core.Interfaces
{
    public interface IPersistenceService
    {
        IEnumerable<Animal> LoadAnimals();
        IEnumerable<Animal> LoadAnimalsForEmployee(int employeeId);
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
        User? GetUserById(int userId);
        bool SaveUser(User user);

        void AddAnimalEvent(int animalId, AnimalEvent ev);
        void DeleteAnimalEvent(int animalId, AnimalEvent ev);
        void UpdateAnimalEvent(int animalId, AnimalEvent oldEvent, AnimalEvent newEvent);
        void DeleteAnimal(int animalId);
    }
}