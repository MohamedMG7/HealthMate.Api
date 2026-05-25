using HealthMate.Application.Manager.BodySiteManager;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HealthMate.Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class BodySiteController : ControllerBase
	{
		private readonly IBodySiteManager _bodySiteManager;
		public BodySiteController(IBodySiteManager bodySiteManager)
		{
			_bodySiteManager = bodySiteManager;
		}

		[HttpGet("Get-All-Body-Sites-Name-Id")]
		public async Task<IActionResult> GetAllBodySitesNameAndId() { 
			var result = await _bodySiteManager.getBodySiteNameAndId();

			return Ok(result);
		}
	}
}
