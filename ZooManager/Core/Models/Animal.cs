using System;
using System.Collections.Generic;

namespace ZooManager.Core.Models
{
    public class Animal
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int SpeciesId { get; set; }
        public string SpeciesName { get; set; }
        public int? EnclosureId { get; set; }
        public string EnclosureName { get; set; }
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
        public List<AnimalEvent> Events { get; set; } = new List<AnimalEvent>();
        public System.DateTime NextFeedingTime { get; set; } 
    }

  
}