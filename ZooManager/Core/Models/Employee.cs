using System.Collections.Generic;

namespace ZooManager.Core.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public List<int> QualifiedSpeciesIds { get; set; } = new List<int>();
    }
}