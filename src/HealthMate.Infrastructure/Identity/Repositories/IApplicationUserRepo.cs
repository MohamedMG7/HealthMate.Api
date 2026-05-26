using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Repositories;

namespace HealthMate.Infrastructure.Identity.Repositories
{
	public interface IApplicationUserRepo : IGenericRepository<ApplicationUser>
	{
		string GetUsernameById(string Id);
	}
}
