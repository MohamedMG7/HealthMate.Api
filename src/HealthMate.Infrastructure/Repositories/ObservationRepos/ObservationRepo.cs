using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Application.Patients.Contracts;
using Microsoft.EntityFrameworkCore;
using HealthMate.Application.Observations.Contracts;
using HealthMate.Application.Ml.Contracts;

namespace HealthMate.Infrastructure.Repositories.ObservationRepos{
    public class ObservationRepo : GenericRepository<Observation>, IObservationRepo{
        private readonly HealthMateContext _context;
        private readonly IDbContextFactory<HealthMateContext> _contextFactory;
        public ObservationRepo(HealthMateContext context,IDbContextFactory<HealthMateContext> contextFactory) : base(context)
        {
            _context = context;
            _contextFactory = contextFactory;
        }


        // this is the first step of the flow return the readings from the database 
        // second is in the observation manager to get the average of the readings + now if this data is updated ===> Handle in the manager
        public async Task<List<HeartRateValueAndDateDto>> GetHeartRateReadingsInXTime(int patientId, int periodInDays)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var endDate = DateTime.Now.AddHours(2); 
            var startDate = endDate.AddDays(-periodInDays);

            var heartRates = await context.Observations
                .Where(o => o.Patient.Id == patientId &&
                           o.CodeDisplayName == "heartrate" &&
                           o.DateOfObservation >= startDate &&
                           o.DateOfObservation <= endDate)
                .Select(o => new HeartRateValueAndDateDto
                {
                    HeartRateValue = o.ValueQuanitity.Value,
                    Date = o.DateOfObservation.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return heartRates;
        }

       public async Task<List<bloodPressureValueAndDateDto>> GetBloodPressureReadingsInXTime(int patientId, int periodInDays)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var endDate = DateTime.UtcNow.AddHours(2); 
            var startDate = endDate.AddDays(-periodInDays);

            var bloodPressureReadings = await context.Observations
                .Where(o => o.Patient.Id == patientId &&
                        o.CodeDisplayName == "bloodpressure" &&
                        o.DateOfObservation >= startDate &&
                        o.DateOfObservation <= endDate)
                .Select(o => new bloodPressureValueAndDateDto
                {
                    Value = o.ValueQuanitity.Value,
                    Date = o.DateOfObservation.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return bloodPressureReadings;
        }

        public async Task<List<HemoglobinValueAndDateDto>> GetHemoglobinDataInXTime(int patientId, int periodInDays)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var endDate = DateTime.Now;
            var startDate = endDate.AddDays(-periodInDays);

            var hemoglobinData = await context.LabTestResults
                .Include(ltr => ltr.LabTestAttribute)
                .Where(ltr => ltr.LabTest.patientId == patientId &&
                            ltr.LabTestAttribute.Name == "hemoglobin" &&
                            ltr.LabTest.RecordedTime >= startDate &&
                            ltr.LabTest.RecordedTime <= endDate)
                .OrderByDescending(ltr => ltr.LabTest.RecordedTime) // Get most recent first
                .Take(7)
                .Select(ltr => new HemoglobinValueAndDateDto
                {
                    HemoglobinValue = ltr.Value,
                    Date = ltr.LabTest.RecordedTime.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return hemoglobinData;
        }

        public async Task<List<GlucoseLevelValueAndDateDto>> GetGlucoseReadingsInXTime(int patientId, int periodInDays)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-periodInDays);
            
            return await _context.Observations
                .Where(o => o.PatientId == patientId 
                            && o.DateOfObservation >= cutoffDate 
                            && o.CodeDisplayName == "glucose") 
                .OrderBy(o => o.DateOfObservation)
                .Select(o => new GlucoseLevelValueAndDateDto
                {
                    GlucoseLevelValue = o.ValueQuanitity ?? 0,
                    Date = o.DateOfObservation.ToString("yyyy-MM-dd")
                })
                .ToListAsync();
        }

        public async Task<List<DocumentDto>> GetMostRecentDocuments(int patientId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Get documents from LabTest, Prescription, and Observation
            var labTestDocuments = await context.LabTests
                .Where(lt => lt.patientId == patientId && lt.ImageUrl != null)
                .Select(lt => new DocumentDto
                {
                    Id = lt.LabTestId,
                    Name = lt.LabTestName,
                    Path = lt.ImageUrl!,
                    RecordedTime = lt.RecordedTime.ToShortDateString()
                })
                .ToListAsync();

            var prescriptionDocuments = await context.Prescriptions
                .Where(p => p.PatientId == patientId && p.PrescriptionImageUrl != null)
                .Select(p => new DocumentDto
                {
                    Id = p.PrescriptionId,
                    Name = "Prescription",
                    Path = p.PrescriptionImageUrl!,
                    RecordedTime = p.PrescriptionDate.ToShortDateString()
                })
                .ToListAsync();

            var MedicalImageDocuments = await context.MedicalImages
                .Where(o => o.paitentId == patientId && o.MedicalImageUrl != null)
                .Select(o => new DocumentDto
                {
                    Id = o.MedicalImageId,
                    Name = o.MedicalImageName ?? "No DATA",
                    Path = o.MedicalImageUrl!,
                    RecordedTime = o.TimeRecorded.ToShortDateString()
                })
                .ToListAsync();

            // Combine all documents
            var allDocuments = labTestDocuments
                .Concat(prescriptionDocuments)
                .Concat(MedicalImageDocuments)
                .OrderByDescending(d => d.RecordedTime) // Sort by most recent
                .Take(2) // Take the top 2
                .ToList();

            return allDocuments;
        }

        public async Task<string> GetHighestBloodPressureAsync(int patientId, int periodInDays)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var result = await context.Observations
                .Where(o => o.PatientId == patientId && o.CodeDisplayName == "bloodpressure" && o.DateOfObservation >= DateTime.Now.AddDays(-periodInDays))
                .MaxAsync(o => o.ValueQuanitity);
            return result.ToString();
        }

        public async Task<string> GetLowestBloodPressureAsync(int patientId, int periodInDays)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var result = await context.Observations
                .Where(o => o.PatientId == patientId && o.CodeDisplayName == "bloodpressure" && o.DateOfObservation >= DateTime.Now.AddDays(-periodInDays))
                .MinAsync(o => o.ValueQuanitity);
            return result.ToString();
        }

        public async Task<AnimeaMLDto> GetRecentCBCTestForML(int patientId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            // Get the most recent CBC test for this patient
            var recentCBCTest = await context.LabTests
                .Where(lt => lt.patientId == patientId && lt.LabTestName.Contains("CBC"))
                .OrderByDescending(lt => lt.RecordedTime)
                .FirstOrDefaultAsync();
            
            if (recentCBCTest == null)
            {
                // No CBC test found for this patient
                return new AnimeaMLDto
                {
                    patientId = patientId
                    // All other values will be 0 by default
                };
            }
            
            // Get ALL results for this test with their attributes
            var labResults = await context.LabTestResults
                .Where(r => r.LabTestId == recentCBCTest.LabTestId)
                .Include(r => r.LabTestAttribute)
                .ToListAsync();
            
            // Map results to DTO using abbreviations
            var result = new AnimeaMLDto
            {
                patientId = patientId,
                
                // HGB - Hemoglobin
                Hemoglobin = labResults
                    .FirstOrDefault(r => r.LabTestAttribute.Abbreviation == "HGB")?.Value ?? 0,
                
                // RBC - Red Blood Cells
                RedBloodCells = labResults
                    .FirstOrDefault(r => r.LabTestAttribute.Abbreviation == "RBC")?.Value ?? 0,
                
                // PCV - Packed Cell Volume
                PackedCellVolume = labResults
                    .FirstOrDefault(r => r.LabTestAttribute.Abbreviation == "PCV")?.Value ?? 0,
                
                // MCH - Mean Corpuscular Hemoglobin
                MeanCorpuscularHemoglobin = labResults
                    .FirstOrDefault(r => r.LabTestAttribute.Abbreviation == "MCH")?.Value ?? 0,
                
                // MCHC - Mean Corpuscular Hemoglobin Concentration
                MeanCorpuscularHemoglobinConcentration = labResults
                    .FirstOrDefault(r => r.LabTestAttribute.Abbreviation == "MCHC")?.Value ?? 0
            };
            
            return result;
        }

        public async Task<decimal> GetLastGlucoseReading(int patientId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            var lastGlucose = await context.Observations
                .Where(o => o.PatientId == patientId && 
                        o.CodeDisplayName == "glucose")
                .OrderByDescending(o => o.DateOfObservation)
                .Select(o => o.ValueQuanitity ?? 0)
                .FirstOrDefaultAsync();

            return lastGlucose;
        }

        // i need to use helper function here to get the blood pressure
        // public async Task<decimal> GetLastBloodPressureReading(int patientId)
        // {
        //     await using var context = await _contextFactory.CreateDbContextAsync();
            
        //     var lastBloodPressure = await context.Observations
        //         .Where(o => o.PatientId == patientId && 
        //                 o.CodeDisplayName == "bloodpressure")
        //         .OrderByDescending(o => o.DateOfObservation)
        //         .Select(o => o.ValueQuanitity ?? 0)
        //         .FirstOrDefaultAsync();

        //     return lastBloodPressure;
        // }

        // get recent data for Diabetes
        // data i need 
        // - last glucose level(observation)
        // - last blood pressure(observation)
        // - insulin(observation)
        // - BMI (Machine Learning Helper)
        // - DiabetesPedigree Function (Machine Learning Helper)
        // - HB1AC (LabTest)
        // - Age (Patient Repo)
    }
}
