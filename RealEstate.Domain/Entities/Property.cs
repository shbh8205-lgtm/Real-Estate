using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstate.Domain.Entities
{
    public class Property
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Address { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Tags { get; set; } = string.Empty; // לאחסון מאפיינים כמו "נוף לים, שקט"
        public float[]? DescriptionVector { get; set; } // לאחסון ה-Embedding של OpenAI
    }
}
