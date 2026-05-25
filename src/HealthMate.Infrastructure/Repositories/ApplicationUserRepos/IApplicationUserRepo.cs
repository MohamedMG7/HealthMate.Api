using HealthMate.Infrastructure.Data.Models;

namespace HealthMate.Infrastructure.Repositories.ApplicationUserRepos
{
	public interface IApplicationUserRepo : IGenericRepository<ApplicationUser>
	{
		string GetUsernameById(string Id);
	}
}
