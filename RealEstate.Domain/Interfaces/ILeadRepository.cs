using RealEstate.Domain.Entities;

namespace RealEstate.Domain.Interfaces;

public interface ILeadRepository : IAsyncRepository<Lead>
{
    Task<IReadOnlyList<Lead>> ListWithDetailsAsync(int? take = null);
}
