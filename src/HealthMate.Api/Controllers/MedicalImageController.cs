using HealthMate.Application.Documents.Contracts;
using HealthMate.Application.Manager.MedicalRecordManager;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HealthMate.Api.Controllers{
    [Authorize(policy:"PatientOrHealthCareProvider")]
    [Route("api/[controller]")]
    [ApiController]
    public class MedicalImageController : ControllerBase{
        private readonly IRecordImageManager _RecordImageManager;
        public MedicalImageController(IRecordImageManager RecordImageManager)
        {
            _RecordImageManager = RecordImageManager;
        }

        
        [HttpPost]
		[Route("UploadMedicalImage")]
        public async Task<IActionResult> UploadMedicalImage([FromForm]MedicalImageAddDto medicalImageDto)
        {
            #region validation
            if(medicalImageDto.image.Length > 5 * 1024 * 1024){
                return StatusCode(StatusCodes.Status400BadRequest,"File size should not exceed 5 MB");
            }

            string[] allowedExtensions = {".jpg",".png",".jpeg",".pdf",".dcm"};
            var fileExtension = Path.GetExtension(medicalImageDto.image.FileName);
            if(!allowedExtensions.Contains(fileExtension)){
                return StatusCode(StatusCodes.Status400BadRequest,"This File Extension Is not Allowed");
            }
            #endregion

            var result = await _RecordImageManager.UploadMedicalImage(medicalImageDto.MedicalImageName, medicalImageDto.image, medicalImageDto.patientId, medicalImageDto.Interpertation);

            if(result){
                return StatusCode(StatusCodes.Status201Created,"Medical Image Uploaded Successfully");
            }

            return StatusCode(StatusCodes.Status500InternalServerError,"Something Went Wrong");
        }
    }
}
