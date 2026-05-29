using HealthMate.Domain.Common.Enums;
using HealthMate.Application.Conditions.Contracts;
using HealthMate.Application.Encounters.Contracts;
using HealthMate.Application.HealthRecord.Contracts;
using HealthMate.Application.LabTests.Contracts;
using HealthMate.Application.Documents.Contracts;
using HealthMate.Application.Prescriptions.Contracts.Medicines;
using HealthMate.Application.Prescriptions.Contracts;

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

		Task<HealthRecordsReadDto> GetAllHealthRecordsAsync(int patientId);
	}
}
