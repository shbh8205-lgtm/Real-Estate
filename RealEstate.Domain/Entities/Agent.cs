using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstate.Domain.Entities
{
    public class Agent
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public List<Property> Listings { get; set; } = new();
        public double AverageRating { get; set; }
    }
}
