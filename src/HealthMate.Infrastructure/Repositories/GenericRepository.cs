using HealthMate.Infrastructure.Data.DbHelper;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Repositories
{
	public class GenericRepository<T> : IGenericRepository<T> where T : class
	{
		private readonly HealthMateContext _Context;
        public GenericRepository(HealthMateContext Context)
        {
            _Context = Context;
        }
        public void Add(T entity)
		{
			_Context.Set<T>().Add(entity);
		}

		public async Task AddAsync(T entity)
		{
			await _Context.Set<T>().AddAsync(entity);
		}

		public void Delete(T entity)
		{
			_Context.Set<T>().Remove(entity);
		}

		public IQueryable<T> GetAll()
		{
			return _Context.Set<T>().AsQueryable();
		}

		public T GetById(int id)
		{
			return _Context.Set<T>().Find(id);
		}

		public void Save()
		{
			_Context.SaveChanges();
		}

		public async Task SaveAsync()
		{
			await _Context.SaveChangesAsync();
		}

		public void Update(T entity)
		{
			_Context.Set<T>().Update(entity);
		}

		public DbContext GetContext()
		{
			return _Context;
		}
	}
}
