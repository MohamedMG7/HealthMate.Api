using HealthMate.Application.Identity.Contracts;

namespace HealthMate.Application.Manager.AdminManager
{
	public interface IAdminManager 
	{
		// show all the requests for unverified users to get verified and all the requests for healthcare providers to make a new account 

		// Patient Request
		IEnumerable<AdminVerifyPatientReadDto> GetPatients();
		void ApproveOrRejectPatient(AdminApprovalDto approvalDto);
		//void VerifyPatient(AdminVerifyPatientReadDto PatientData);

		// Healthcare Providers

	}
}
