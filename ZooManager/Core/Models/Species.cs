namespace ZooManager.Core.Models;

public class Species
{
    public int Id { get; set; }
    public string Name { get; set; }
    
        public string RequiredClimate { get; set; }
        public bool NeedsWater { get; set; }
        public double MinSpacePerAnimal { get; set; }

        public List<SpeciesFieldDefinition> CustomFields { get; set; } = new List<SpeciesFieldDefinition>();
    }