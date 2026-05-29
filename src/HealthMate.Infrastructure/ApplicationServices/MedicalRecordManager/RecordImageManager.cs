using HealthMate.Application.Abstractions.Storage;
using HealthMate.Domain.Aggregates.Prescription;
using HealthMate.Domain.Common;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace HealthMate.Application.Manager.MedicalRecordManager{
    public class RecordImageManager : IRecordImageManager{
        private readonly IGenericRepository<LabTest> _LabTestRepo;
		private readonly IGenericRepository<Prescription> _PrescriptionRepo;
		private readonly IGenericRepository<MedicalImage> _MedicalImageRepo;
		private readonly IFileStorage _fileStorage;
		private readonly IDateTimeProvider _clock;
		public RecordImageManager(IGenericRepository<LabTest> LabTest,IFileStorage fileStorage, IGenericRepository<Prescription> Prescription,IGenericRepository<MedicalImage> MedicalImage, IDateTimeProvider clock)
		{
			_LabTestRepo = LabTest;
			_fileStorage = fileStorage;
			_PrescriptionRepo = Prescription;
			_MedicalImageRepo = MedicalImage;
			_clock = clock;
		}


        // record types {imaging .. prescription .. labtest} 
        [Authorize(Policy="RequirePatientRole")]
        public async Task<bool> UploadLabTestImage(string medicalRecordName, IFormFile image, int patientId){
            try {
                
                    var filePath = await SaveFormFileAsync(image, "LabTests", $"{patientId}_{medicalRecordName}_{DateTime.Now:yyyyMMdd}");

                    var labtest = new LabTest{
                        patientId = patientId,
                        LabTestName = medicalRecordName,
                        RecordedTime = DateTime.Now,
                        ImageUrl = filePath 
                    };

                    await _LabTestRepo.AddAsync(labtest);
                    await _LabTestRepo.SaveAsync();
                    return true;

            }
            catch {
                return false;
            }
        }

        public async Task<bool> UploadPrescriptionImage(string medicalRecordName, IFormFile image, int patientId)
        {
            try
            {
                var filePath = await SaveFormFileAsync(image, "Prescriptions", $"{patientId}_{medicalRecordName}_{DateTime.Now:yyyyMMdd}");

				var prescription = Prescription.UploadImage(patientId, medicalRecordName, filePath, _clock);

                // TODO: Add prescription medicines logic when needed
                await _PrescriptionRepo.AddAsync(prescription);
                await _PrescriptionRepo.SaveAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UploadMedicalImage(string medicalImageName, IFormFile image, int patientId, string interpretation)
        {
            try
            {
                var filePath = await SaveFormFileAsync(image, "MedicalImages", $"{patientId}_{medicalImageName}_{DateTime.Now:yyyyMMdd}");

                var medicalImage = new MedicalImage
                {
                    paitentId = patientId,
                    MedicalImageName = medicalImageName,
                    MedicalImageUrl = filePath,
                    TimeRecorded = DateTime.UtcNow,
                    Interpertation = interpretation
                };

                // TODO: Add repository logic when needed
                await _MedicalImageRepo.AddAsync(medicalImage);
                await _MedicalImageRepo.SaveAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> SaveFormFileAsync(IFormFile image, string folder, string savedFileName)
        {
            var fileName = savedFileName + Path.GetExtension(image.FileName);
            await using var stream = image.OpenReadStream();
            return await _fileStorage.SaveAsync(stream, image.ContentType, folder, fileName);
        }
    }
}
