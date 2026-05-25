using HealthMate.Infrastructure.Data.Models;

namespace HealthMate.Infrastructure.Repositories.AdminRepos
{
	public interface IAdminRepo
	{
		Patient GetPatientWithApplicationUserData(int id);
	}
}
