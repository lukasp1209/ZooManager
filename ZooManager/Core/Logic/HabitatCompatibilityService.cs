using System;
using ZooManager.Core.Models;

namespace ZooManager.Core.Logic
{
    public class HabitatCompatibilityService
    {
        public bool IsCompatible(Species species, Enclosure enclosure)
        {
            if (species == null || enclosure == null)
                return false;
            
            return string.Equals(
                species.HabitatType,
                enclosure.HabitatType,
                StringComparison.OrdinalIgnoreCase
            );
        }
    }
}