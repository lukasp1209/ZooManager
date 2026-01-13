namespace ZooManager.Core.Models;

public class Species
{
    public int Id { get; set; }
    public string CommonName { get; set; }
    public string ScientificName { get; set; }
    public string HabitatType { get; set; } // e.g. Savanna, Rainforest
}