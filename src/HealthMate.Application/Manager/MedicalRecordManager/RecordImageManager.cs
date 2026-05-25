using HealthMate.Application.Services;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using static System.Net.Mime.MediaTypeNames;

namespace HealthMate.Application.Manager.MedicalRecordManager{
    public class RecordImageManager : IRecordImageManager{
        private readonly IGenericRepository<LabTest> _LabTestRepo;
        private readonly IGenericRepository<Prescription> _PrescriptionRepo;
        private readonly IGenericRepository<MedicalImage> _MedicalImageRepo;
        private readonly IFileService _FileService;
        public RecordImageManager(IGenericRepository<LabTest> LabTest,IFileService fileService, IGenericRepository<Prescription> Prescription,IGenericRepository<MedicalImage> MedicalImage)
        {
            _LabTestRepo = LabTest;
            _FileService = fileService;
            _PrescriptionRepo = Prescription;
            _MedicalImageRepo = MedicalImage;
        }


        // record types {imaging .. prescription .. labtest} 
        [Authorize(Policy="RequirePatientRole")]
        public async Task<bool> UploadLabTestImage(string medicalRecordName, IFormFile image, int patientId){
            try {
                
                    var filePath = await _FileService.SaveFileAsync(image, "LabTests", 
                        $"{patientId}_{medicalRecordName}_{DateTime.Now:yyyyMMdd}"
                    );

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
                var filePath = await _FileService.SaveFileAsync(image, "Prescriptions",
                    $"{patientId}_{medicalRecordName}_{DateTime.Now:yyyyMMdd}"
                );

                var prescription = new Prescription
                {
                    PatientId = patientId,
                    Publisher = medicalRecordName,
                    PrescriptionDate = DateTime.Now,
					PrescriptionImageUrl = filePath
                };

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
                var filePath = await _FileService.SaveFileAsync(image, "MedicalImages",
                    $"{patientId}_{medicalImageName}_{DateTime.Now:yyyyMMdd}"
                );

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
    }
}
