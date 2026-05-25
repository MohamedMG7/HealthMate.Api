using HealthMate.Infrastructure.DTO.ConditionDto;
using HealthMate.Infrastructure.DTO.EncounterDto;
using HealthMate.Infrastructure.DTO.LabTestDto;
using HealthMate.Infrastructure.DTO.MedicalImageDto;
using HealthMate.Infrastructure.DTO.MedicineDto;
using HealthMate.Infrastructure.DTO.PrescriptionDto;
using HealthMate.Infrastructure.Enums;

namespace HealthMate.Infrastructure.Repositories.HealthRecordRepos
{
	public interface IHealthRecordRepo
	{
		Task<(string Name, DateOnly BirthDate, float? Weight, float? Height, Gender Gender)> GetPatientGeneralInfoAsync(int patientId);

		// Lab Tests
		Task<List<LabTestSummaryReadDto>> getLabTestsSummary(int patientId);

		// Medical Images
		Task<List<MedicalImageSummaryReadDto>> getMedicalImagesSummary(int patientId);

		// Medicines
		Task<List<MedicineSummaryReadDto>> getMedicineSummary(int patientId);

		// Conditions
		Task<List<ConditionSummaryReadDto>> getConditionsSummary(int patientId);

		// Prescriptions
		Task<List<PrescriptionSummaryReadDto>> getPrescriptionsSummary(int patientId);
		//Encounters
		Task<List<EncounterSumaryReadDto>> getEncounterSummary(int patientId);

		// Medical Images Details
		Task<MedicalImageDetailsReadDto> getMedicalImageDetails(int MedicalImageId);

		// Prescription Details
		Task<PrescriptionDetailsReadDto> getPrescriptionDetails(int prescriptionId);

		// LabTest Details
		Task<LabTestDetailsReadDto> getLabTestDetails(int labTestId);
		// Encounter Details
		Task<EncounterDetailsDto> getEncounterDetails(int encounterId);
	}
}
