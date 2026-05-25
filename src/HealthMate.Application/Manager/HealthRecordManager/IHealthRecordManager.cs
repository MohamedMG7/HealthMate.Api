using HealthMate.Infrastructure.DTO.EncounterDto;
using HealthMate.Infrastructure.DTO.HealthRecordDto;
using HealthMate.Infrastructure.DTO.LabTestDto;
using HealthMate.Infrastructure.DTO.MedicalImageDto;
using HealthMate.Infrastructure.DTO.PrescriptionDto;


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
