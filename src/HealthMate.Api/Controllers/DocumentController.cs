using Microsoft.AspNetCore.Mvc;
using HealthMate.Application.Manager.DocumentManager;
using System.Threading.Tasks;
using System.IO;

namespace HealthMate.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentManager _documentManager;

        public DocumentController(IDocumentManager documentManager)
        {
            _documentManager = documentManager;
        }

        [HttpGet("download-file")]
		public async Task<IActionResult> GetFile([FromQuery] string filePath)
		{
			try
			{
				var fileResult = await _documentManager.GetFileAsync(filePath);
				return fileResult;
			}
			catch (FileNotFoundException ex)
			{
				return NotFound(ex.Message);
			}
			catch (Exception ex)
			{
				// This helps you debug
				return StatusCode(500, $"Internal error: {ex.Message}");
			}
		}
	}
}