using ZooManager.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace ZooManager.Core.Services
{
    public class EnclosureValidationService
    {
        public bool IsSuitable(Animal animal, Species species, Enclosure enclosure, int currentAnimalCountInEnclosure)
        {
            if (species.RequiredClimate != enclosure.ClimateType)
                return false;

            if (species.NeedsWater && !enclosure.HasWaterAccess)
                return false;

            double requiredSpace = species.MinSpacePerAnimal * (currentAnimalCountInEnclosure + 1);
            if (enclosure.TotalArea < requiredSpace)
                return false;

            if (currentAnimalCountInEnclosure >= enclosure.MaxCapacity)
                return false;

            return true;
        }

        public string GetIncompatibilityReason(Animal animal, Species species, Enclosure enclosure, int count)
        {
            if (species.RequiredClimate != enclosure.ClimateType)
                return $"Klima ungeeignet: Benötigt {species.RequiredClimate}, Gehege ist {enclosure.ClimateType}.";
            
            if (species.NeedsWater && !enclosure.HasWaterAccess)
                return "Gehege bietet keinen erforderlichen Wasserzugang.";
            
            if (enclosure.TotalArea < (species.MinSpacePerAnimal * (count + 1)))
                return "Gehege ist zu klein für ein weiteres Tier dieser Art.";

            return "Unbekannter Fehler bei der Zuordnung.";
        }
    }
}