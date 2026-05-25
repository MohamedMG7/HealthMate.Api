
using HealthMate.Infrastructure.DTO.UserDto;
using HealthMate.Infrastructure.Data.Models;

namespace HealthMate.Application.Manager.UsersManager
{
	public interface IUserManager
	{
		IEnumerable<UserReadDto> GetAll();
		IEnumerable<ActiveUserReadDto> GetAllActive();
		string GetUserNameById(string Id);
	}
}
