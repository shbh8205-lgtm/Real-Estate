using Microsoft.EntityFrameworkCore;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Interfaces;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Repositories;

public class LeadRepository : EfRepository<Lead>, ILeadRepository
{
    public LeadRepository(ApplicationDbContext db) : base(db) { }

    public async Task<IReadOnlyList<Lead>> ListWithDetailsAsync(int? take = null)
    {
        var query = _dbContext.Set<Lead>()
            .Include(l => l.Property)
            .Include(l => l.Client)
            .OrderByDescending(l => l.CreatedAt)
            .AsQueryable();

        if (take.HasValue) query = query.Take(take.Value);

        return await query.ToListAsync();
    }
}
