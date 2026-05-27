using HealthMate.Application.Encounters.Contracts;
using HealthMate.Application.HealthRecord.Contracts;
using HealthMate.Application.LabTests.Contracts;
using HealthMate.Application.Documents.Contracts;
using HealthMate.Application.Prescriptions.Contracts;


namespace HealthMate.Application.Manager.HealthRecordManager
{
	public interface IHealthRecordManager
	{
		// this should have 
		//General information
		Task<GeneralPatientInformationReadDto> GetPatientGeneralInformation(int PatientId);

		//important behaviours like if he is a smoke we should show this? 

		//allergies 

		// HealthTrend

		// All Health Records
		Task<HealthRecordsReadDto> GetAllHealthRecords(int patientId);

		Task<HealthRecordSummaryDto> GetAllHealthRecordSummary(int patientId);

		#region Health Record Details
		Task<LabTestDetailsReadDto> GetLabTestDetailsAsync(int labTestId);
		Task<MedicalImageDetailsReadDto> GetMedicalImageDetailsAsync(int medicalImageId);
		Task<PrescriptionDetailsReadDto> GetPrescriptionDetailsAsync(int PrescriptionId);
		Task<EncounterDetailsDto> GetEncounterDetailsAsync(int encounterId);
		#endregion

		
		
	}
}
