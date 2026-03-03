using System.Collections.Generic;
using ZooManager.Core.Models;

namespace ZooManager.Core.Interfaces;

public interface IZooRepository
{
    IEnumerable<Species> GetAllSpecies();
    void DeleteSpecies(int id);
    bool HasAnimalsForSpecies(int speciesId);
}