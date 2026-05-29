using HealthMate.Application.Encounters.Contracts;
using HealthMate.Application.HealthRecord.Contracts;
using HealthMate.Application.Prescriptions.Contracts;
using HealthMate.Application.Documents.Contracts;
using HealthMate.Application.LabTests.Contracts;
using HealthMate.Infrastructure.Repositories.HealthRecordRepos;
using HealthMate.Application.Manager.UtilityManager;

namespace HealthMate.Application.Manager.HealthRecordManager
{
	public class HealthRecordManager : IHealthRecordManager
	{
		private readonly IHealthRecordRepo _healthRecordRepo;
		private readonly IUtilityManager _utiliyManager;
        public HealthRecordManager(IUtilityManager utilityManager, IHealthRecordRepo healthRecordRepo)
        {
			_healthRecordRepo = healthRecordRepo;
			_utiliyManager = utilityManager;
        }

		public async Task<HealthRecordsReadDto> GetAllHealthRecords(int patientId)
		{
			return await _healthRecordRepo.GetAllHealthRecordsAsync(patientId);
		}

		public async Task<GeneralPatientInformationReadDto> GetPatientGeneralInformation(int PatientId)
		{
			var rawData = await _healthRecordRepo.GetPatientGeneralInfoAsync(PatientId);

			var patientGeneralInformation = new GeneralPatientInformationReadDto {
				Name = rawData.Name,
				Age = _utiliyManager.CalculateAgeReturnYearsOnly(rawData.BirthDate),
				Gender = rawData.Gender.ToString(),
				Height = rawData.Height.HasValue ? rawData.Height.Value.ToString() : "N/A",
				Weight = rawData.Weight.HasValue ? rawData.Weight.Value.ToString() : "N/A"
			};

			return patientGeneralInformation;
		}

		public async Task<HealthRecordSummaryDto> GetAllHealthRecordSummary(int patientId){

			if (patientId <= 0)
			{
				throw new ArgumentException("Invalid patient ID", nameof(patientId));
			}

			// Execute all async operations in parallel
			var labTestsTask = _healthRecordRepo.getLabTestsSummary(patientId);
			var medicalImagesTask = _healthRecordRepo.getMedicalImagesSummary(patientId);
			var medicinesTask = _healthRecordRepo.getMedicineSummary(patientId);
			var conditionsTask = _healthRecordRepo.getConditionsSummary(patientId);
			var prescriptionsTask = _healthRecordRepo.getPrescriptionsSummary(patientId);
			var encountersTask = _healthRecordRepo.getEncounterSummary(patientId);

			// Wait for all tasks to complete
			await Task.WhenAll(
				labTestsTask,
				medicalImagesTask,
				medicinesTask,
				conditionsTask,
				prescriptionsTask,
				encountersTask
			);

			return new HealthRecordSummaryDto
			{
				LabTestsSummary = await labTestsTask,
				MedicalImagesSummary = await medicalImagesTask,
				MedicinesSummary = await medicinesTask,
				ConditionsSummary = await conditionsTask,
				PrescriptionsSummary = await prescriptionsTask,
				EncountersSummary = await encountersTask
			};
		}

		#region HealthRecords Details
		public async Task<PrescriptionDetailsReadDto> GetPrescriptionDetailsAsync(int PrescriptionId)
		{
			if (PrescriptionId <= 0)
				throw new ArgumentException("Invalid patient ID", nameof(PrescriptionId));

			return await _healthRecordRepo.getPrescriptionDetails(PrescriptionId);
		}

		public async Task<MedicalImageDetailsReadDto> GetMedicalImageDetailsAsync(int medicalImageId)
		{
			if (medicalImageId <= 0)
				throw new ArgumentException("Invalid medical image ID", nameof(medicalImageId));

			return await _healthRecordRepo.getMedicalImageDetails(medicalImageId);
		}

		public async Task<LabTestDetailsReadDto> GetLabTestDetailsAsync(int labTestId)
		{
			if (labTestId <= 0)
				throw new ArgumentException("Invalid lab test ID", nameof(labTestId));

			return await _healthRecordRepo.getLabTestDetails(labTestId);
		}

		public async Task<EncounterDetailsDto> GetEncounterDetailsAsync(int encounterId)
		{
			if (encounterId <= 0)
				throw new ArgumentException("Invalid encounter ID", nameof(encounterId));

			return await _healthRecordRepo.getEncounterDetails(encounterId);
		}
		#endregion
	}
}
