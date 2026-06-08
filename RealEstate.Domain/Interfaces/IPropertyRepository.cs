using RealEstate.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstate.Domain.Interfaces
{
    public interface IPropertyRepository : IAsyncRepository<Property>
    {
        Task<Property?> GetByIdAsync(Guid id);
        Task<IEnumerable<Property>> GetAllAsync();
        Task AddAsync(Property property);
        Task<IReadOnlyList<Property>> GetPropertiesByPriceRangeAsync(decimal minPrice, decimal maxPrice);
    }
}
