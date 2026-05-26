using HealthMate.Application.Abstractions.Identity.Ports;
using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Domain.Common;
using HealthMate.Infrastructure.DTO.AdminDto;
using HealthMate.Application.Manager.PatientManager;
using HealthMate.Infrastructure.Data.DbHelper;
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
        private readonly IDateTimeProvider _clock;
        public AdminManager(IGenericRepository<Patient> patientManager, IEmailService emailService, IAdminRepo adminRepo, IDateTimeProvider clock)
        {
            _patientManager = patientManager;
			_emailService = emailService;
			_adminRepo = adminRepo;
            _clock = clock;
        }

		public void ApproveOrRejectPatient(AdminApprovalDto approvalDto)
		{

			// i need to send emails with confirmation or not

			var patient = _adminRepo.GetPatientWithApplicationUserData(approvalDto.PatientId);

			if (patient == null)
				throw new KeyNotFoundException("Patient not found.");

            var context = (HealthMateContext)_patientManager.GetContext();
            string patientEmail = context.Users
                .Where(user => user.Id == patient.ApplicationUserId)
                .Select(user => user.Email!)
                .FirstOrDefault() ?? throw new KeyNotFoundException("Patient account not found.");

			if (patient.IsVerified == false)
			{
				if (approvalDto.IsApproved)
				{
					patient.Verify(_clock);
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
			var context = (HealthMateContext)_patientManager.GetContext();
			var PatientsModed = _patientManager.GetAll().Where(p => p.IsVerified == false).ToList(); // get all the unverified patients
			var userIds = PatientsModed
				.Select(p => p.ApplicationUserId)
				.Where(id => !string.IsNullOrWhiteSpace(id))
				.Distinct()
				.ToArray();
			var users = context.Users
				.Where(user => userIds.Contains(user.Id))
				.ToDictionary(user => user.Id);

			var PatientsList = PatientsModed.Select(x =>
			{
				ApplicationUser? user = null;
				var hasUser = x.ApplicationUserId is not null && users.TryGetValue(x.ApplicationUserId, out user);
				return new AdminVerifyPatientReadDto
				{
					Id = x.Patient_Id,
					First_Name = hasUser ? user!.First_Name : "No Data",
					Last_Name = hasUser ? user!.Last_Name : "No Data",
					NationalIdNumber = x.NationalId.Value,
					NatinoalIDImageUrl = x.NationalIdImageUrl,
					ApplicationUserImageUrl = hasUser ? user!.ImageUrl : ""
				};
			});

			return PatientsList;
		}
	}
}
