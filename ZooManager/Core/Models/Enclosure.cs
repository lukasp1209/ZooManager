namespace ZooManager.Core.Models
{
    public class Enclosure
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ClimateType { get; set; }
        public bool HasWaterAccess { get; set; }
        public double TotalArea { get; set; }
        public int MaxCapacity { get; set; }
    }
}