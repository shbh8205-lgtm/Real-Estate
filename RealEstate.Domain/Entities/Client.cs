using RealEstate.Domain.Entities;
using System.Collections.Generic;

namespace RealEstate.Domain.Entities
{
    public class Client
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public ClientRole Role { get; set; } = ClientRole.Buyer;
        public List<Property> SavedProperties { get; set; } = new();
        public decimal MaxBudget { get; set; } = 0;
    }

    public enum ClientRole
    {
        Buyer,
        Renter,
        Agent
    }
}
