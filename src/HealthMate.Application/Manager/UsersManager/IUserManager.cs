using HealthMate.Application.Identity.Contracts.Users;

namespace HealthMate.Application.Manager.UsersManager
{
	public interface IUserManager
	{
		IEnumerable<UserReadDto> GetAll();
		IEnumerable<ActiveUserReadDto> GetAllActive();
		string GetUserNameById(string Id);
	}
}
