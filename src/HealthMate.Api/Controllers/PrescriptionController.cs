using HealthMate.Application.Manager.MedicalRecordManager;
using HealthMate.Application.Prescriptions.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HealthMate.Api.Controllers
{
    [Authorize(policy:"PatientOrHealthCareProvider")]
    [Route("api/[controller]")]
    [ApiController]
    public class PrescriptionController : ControllerBase
    {
        private readonly IRecordImageManager _RecordImageManager;

        public PrescriptionController(IRecordImageManager recordImageManager)
        {
            _RecordImageManager = recordImageManager;
        }

        [HttpPost]
        [Route("UploadPrescriptionImage")]
        public async Task<IActionResult> UploadPrescriptionImage([FromForm]PrescriptionAddDto prescriptionDto)
        {
            #region validation
            if(prescriptionDto.image.Length > 5 * 1024 * 1024)
            {
                return StatusCode(StatusCodes.Status400BadRequest, "File size should not exceed 5 MB");
            }

            string[] allowedExtensions = {".jpg",".png",".jpeg"};
            var fileExtension = Path.GetExtension(prescriptionDto.image.FileName);
            if(!allowedExtensions.Contains(fileExtension))
            {
                return StatusCode(StatusCodes.Status400BadRequest, "This File Extension Is not Allowed");
            }
            #endregion

            var result = await _RecordImageManager.UploadPrescriptionImage(prescriptionDto.medicalRecordName, prescriptionDto.image, prescriptionDto.patientId);

            if(result)
            {
                return StatusCode(StatusCodes.Status201Created, "Prescription Image Uploaded Successfully");
            }

            return StatusCode(StatusCodes.Status500InternalServerError, "Something Went Wrong");
        }
    }
}
