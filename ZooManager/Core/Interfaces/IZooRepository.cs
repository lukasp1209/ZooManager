using System.Collections.Generic;
using ZooManager.Core.Models;

namespace ZooManager.Core.Interfaces;

public interface IZooRepository
{
    IEnumerable<Species> GetAllSpecies();
    void SaveSpecies(Species species);
    void DeleteSpecies(int id);
    bool HasAnimalsForSpecies(int speciesId);
    
    IEnumerable<Employee> GetAllEmployees();
    void SaveEmployee(Employee employee);
    void DeleteEmployee(int id);
    void UpdateEmployeeQualifications(int employeeId, List<int> speciesIds);
}