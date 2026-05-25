using HealthMate.Infrastructure.DTO.UserDto;
using HealthMate.Application.Manager.UsersManager;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HealthMate.Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class UsersController : ControllerBase
	{
        private readonly IUserManager _userManager;
        public UsersController(IUserManager userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
			var users = _userManager.GetAll();

			if (users == null || !users.Any()) {
				return NotFound("No Users Found");
			}

            return Ok(users);
        }

		[HttpGet]
		[Route("ActiveUsers")]
		public IActionResult GetAllActive()
		{
			var activeUsers = _userManager.GetAllActive();

			if (activeUsers == null || !activeUsers.Any())
			{
				return NotFound("No active users found."); 
			}

			return Ok(activeUsers); 
		}

		[HttpGet]
		[Route("Username")]
		public IActionResult GetUsernameById(string Id)
		{
			try
			{
				// Call the BLL to get the user name
				var userName = _userManager.GetUserNameById(Id);

				// Return a successful response with the user name
				return Ok(new { Username = userName });
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(new { Message = ex.Message });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { Message = "An unexpected error occurred.", Details = ex.Message });
			}
		}
	}
}
