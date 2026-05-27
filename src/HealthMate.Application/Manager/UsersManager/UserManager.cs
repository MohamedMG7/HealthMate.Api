using HealthMate.Application.Abstractions.Identity.Ports;
using HealthMate.Application.Identity.Contracts.Users;
using HealthMate.Application.Abstractions.Enums;

namespace HealthMate.Application.Manager.UsersManager
{
	public class UserManager : IUserManager
	{
		private readonly IIdentityUserDirectory _userDirectory;
        public UserManager(IIdentityUserDirectory userDirectory)
        {
            _userDirectory = userDirectory;
        }

		public IEnumerable<UserReadDto> GetAll()
		{
			var users =  _userDirectory.GetAll();
			var userList = users.Select(x => new UserReadDto { 
				Id = x.Id,
				Email =	x.Email,
				EmailConfirmed = x.EmailConfirmed,
				First_Name = x.FirstName,
				Last_Name = x.LastName,
				ImageUrl = x.ImageUrl,
				UserType = (UserType)x.UserType,
				IsActive = x.IsActive
			});

			return userList;
		}

		public IEnumerable<ActiveUserReadDto> GetAllActive()
		{
			var users = _userDirectory.GetAllActive();
			var userList = users.Select(x => new ActiveUserReadDto
			{
				Id = x.Id,
				Email = x.Email,
				EmailConfirmed = x.EmailConfirmed,
				First_Name = x.FirstName,
				Last_Name = x.LastName,
				ImageUrl = x.ImageUrl,
				UserType = (UserType)x.UserType
			});
			return userList;
		}

		public string GetUserNameById(string id)
		{
			string? userName = _userDirectory.GetUserNameById(id);

			if (userName == null)
				throw new KeyNotFoundException("User not found.");

			return userName;
		}
	}
}
