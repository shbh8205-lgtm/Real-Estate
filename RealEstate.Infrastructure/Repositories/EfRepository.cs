using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Domain.Interfaces
{
	public class EfRepository<T> : IAsyncRepository<T> where T : class
	{
		protected readonly ApplicationDbContext _dbContext;

		public EfRepository(ApplicationDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		public virtual async Task<T?> GetByIdAsync(int id) => await _dbContext.Set<T>().FindAsync(id);

		public async Task<IReadOnlyList<T>> ListAllAsync() => await _dbContext.Set<T>().ToListAsync();

		public async Task<T> AddAsync(T entity)
		{
			await _dbContext.Set<T>().AddAsync(entity);
			await _dbContext.SaveChangesAsync();
			return entity;
		}

		public async Task UpdateAsync(T entity)
		{
			_dbContext.Entry(entity).State = EntityState.Modified;
			await _dbContext.SaveChangesAsync();
		}

		public async Task DeleteAsync(T entity)
		{
			_dbContext.Set<T>().Remove(entity);
			await _dbContext.SaveChangesAsync();
		}
	}
}
