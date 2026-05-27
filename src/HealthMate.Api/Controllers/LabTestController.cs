using System.Threading.Tasks;
using HealthMate.Application.LabTests.Contracts;
using HealthMate.Application.Manager.LabTestManager;
using HealthMate.Application.Manager.MedicalRecordManager;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HealthMate.Api.Controllers{
    [Authorize(policy:"PatientOrHealthCareProvider")]
    [Route("api/[controller]")]
    [ApiController]
    public class LabTestController : ControllerBase{
        private readonly ILabTestManager _LabTestManager;
        private readonly IRecordImageManager _RecordImageManager;

        public LabTestController(ILabTestManager labTestManager, IRecordImageManager recordImageManager)
        {
            _LabTestManager = labTestManager;
            _RecordImageManager = recordImageManager;
        }

        [Route("AddTest")]
        [HttpPost]
        public async Task<IActionResult> AddLabTest([FromBody]LabTestAddDto LabTestDto)
        {
            try
            {
                await _LabTestManager.addLabTestAsync(LabTestDto);
                return StatusCode(StatusCodes.Status201Created);
            }
            catch
            {
                return BadRequest("Failed to add lab test");
            }
        }

        [Route("UploadLabTestImage")]
        [HttpPost]
        public async Task<IActionResult> UploadLabTestImage([FromForm]LabTestImageDto LabTestDto){
            
            #region validation

            if (LabTestDto?.Image == null)
            {
                return BadRequest("Where is the image");
            }

            if(LabTestDto.Image!.Length > 5 * 1024 * 1024){
				return StatusCode(StatusCodes.Status400BadRequest,"File size should not exceed 5 MB");
			}

			string[] allowedExtensions = {".jpg",".png",".jpeg",".pdf"};
			var fileExtension = Path.GetExtension(LabTestDto.Image.FileName);
			if(!allowedExtensions.Contains(fileExtension)){
				return StatusCode(StatusCodes.Status400BadRequest,"This File Extension Is not Allowed");
			}


            #endregion

            var result = await _RecordImageManager.UploadLabTestImage(LabTestDto.LabTestName,LabTestDto.Image,LabTestDto.patientId);

            if(result){
                return StatusCode(StatusCodes.Status201Created,"Record Added");
            }

            return StatusCode(StatusCodes.Status500InternalServerError,"Something Wrong Happened");
        }
    }    
}