using HealthMate.Infrastructure.DTO.AdminDto;
using HealthMate.Application.Manager.AccountManager;
using HealthMate.Application.Manager.PatientManager;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Repositories;
using HealthMate.Infrastructure.Repositories.AdminRepos;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Application.Manager.AdminManager
{
	public class AdminManager : IAdminManager
	{
		private readonly IGenericRepository<Patient> _patientManager;
		private readonly IEmailService _emailService;
		private readonly IAdminRepo _adminRepo;
        public AdminManager(IGenericRepository<Patient> patientManager, IEmailService emailService, IAdminRepo adminRepo)
        {
            _patientManager = patientManager;
			_emailService = emailService;
			_adminRepo = adminRepo;
        }

		public void ApproveOrRejectPatient(AdminApprovalDto approvalDto)
		{

			// i need to send emails with confirmation or not

			var patient = _adminRepo.GetPatientWithApplicationUserData(approvalDto.PatientId);
			string patientEmail = patient.ApplicationUser.Email;

			if (patient == null)
				throw new KeyNotFoundException("Patient not found.");
			if (patient.IsVerified == false)
			{
				if (approvalDto.IsApproved)
				{
					patient.IsVerified = true;
					_patientManager.Update(patient);

					// send email to the user 
					string emailBody = "Your Record Now Is Verified You Can use the system now";
					_emailService.SendEmailAsync(patientEmail, "Email Verified", emailBody);
				}
				else
				{
					// Log rejection reason or perform additional actions
					var rejectionReason = approvalDto.RejectionReason;
					if (string.IsNullOrWhiteSpace(rejectionReason))
						throw new ArgumentException("Rejection reason is required when rejecting a patient.");
					_emailService.SendEmailAsync(patientEmail, "Email Verification Need Your Attention", rejectionReason);
				}

				_patientManager.Save();
			}
			else {
				throw new InvalidOperationException("Already Verified");
			}
			
		}

		public IEnumerable<AdminVerifyPatientReadDto> GetPatients()
		{
			var PatientsModed = _patientManager.GetAll().Include(p => p.ApplicationUser).Where(p => p.IsVerified == false).ToList(); // get all the unverified patients

			var PatientsList = PatientsModed.Select(x => new AdminVerifyPatientReadDto { 
				Id = x.Patient_Id,
				First_Name = x.ApplicationUser.First_Name,
				Last_Name = x.ApplicationUser.Last_Name,
				NationalIdNumber = x.NationalId,
				NatinoalIDImageUrl = x.NationalIdImageUrl,
				ApplicationUserImageUrl = x.ApplicationUser.ImageUrl
			});

			return PatientsList;
		}
	}
}
