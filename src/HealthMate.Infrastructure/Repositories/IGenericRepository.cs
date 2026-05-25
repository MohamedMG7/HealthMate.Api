using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Repositories
{
	public interface IGenericRepository<T> where T : class
	{
		T GetById(int id);
		IQueryable<T> GetAll();
		void Add(T entity);
		void Update(T entity);
		void Delete(T entity);
		void Save();
		DbContext GetContext();
		Task SaveAsync();
		Task AddAsync(T entity);
	}
}
