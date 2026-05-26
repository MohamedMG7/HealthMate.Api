using HealthMate.Infrastructure.DTO.UserDto;
using HealthMate.Infrastructure.Identity.Repositories;

namespace HealthMate.Application.Manager.UsersManager
{
	public class UserManager : IUserManager
	{
		private readonly IApplicationUserRepo _userRepo;
        public UserManager(IApplicationUserRepo userRepo)
        {
            _userRepo = userRepo;
        }

		public IEnumerable<UserReadDto> GetAll()
		{
			var users =  _userRepo.GetAll().ToList();
			var userList = users.Select(x => new UserReadDto { 
				Id = x.Id,
				Email =	x.Email,
				EmailConfirmed = x.EmailConfirmed,
				First_Name = x.First_Name,
				Last_Name = x.Last_Name,
				ImageUrl = x.ImageUrl,
				UserType = x.UserType,
				IsActive = x.IsActive
			});

			return userList;
		}

		public IEnumerable<ActiveUserReadDto> GetAllActive()
		{
			var users = _userRepo.GetAll().Where(x => x.IsActive == true).ToList();
			var userList = users.Select(x => new ActiveUserReadDto
			{
				Id = x.Id,
				Email = x.Email,
				EmailConfirmed = x.EmailConfirmed,
				First_Name = x.First_Name,
				Last_Name = x.Last_Name,
				ImageUrl = x.ImageUrl,
				UserType = x.UserType
			});
			return userList;
		}

		public string GetUserNameById(string id)
		{
			string userName = _userRepo.GetUsernameById(id);

			if (userName == null)
				throw new KeyNotFoundException("User not found.");

			return _userRepo.GetUsernameById(id);
		}
	}
}
